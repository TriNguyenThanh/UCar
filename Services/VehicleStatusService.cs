using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý trạng thái xe với state machine validation
/// </summary>
public class VehicleStatusService : IVehicleStatusService
{
    private readonly UCarDbContext _context;

    /// <summary>
    /// State machine: định nghĩa các chuyển trạng thái hợp lệ
    /// </summary>
    private static readonly Dictionary<VehicleStatus, VehicleStatus[]> AllowedTransitions = new()
    {
        [VehicleStatus.Available] = new[] 
        { 
            VehicleStatus.Reserved, 
            VehicleStatus.Maintenance, 
            VehicleStatus.Decommissioned 
        },
        [VehicleStatus.Reserved] = new[] 
        { 
            VehicleStatus.Renting, 
            VehicleStatus.Available 
        },
        [VehicleStatus.Renting] = new[] 
        { 
            VehicleStatus.Available, 
            VehicleStatus.Incident, 
            VehicleStatus.Impounded 
        },
        [VehicleStatus.Maintenance] = new[] 
        { 
            VehicleStatus.Available 
        },
        [VehicleStatus.Impounded] = new[] 
        { 
            VehicleStatus.Available, 
            VehicleStatus.Incident 
        },
        [VehicleStatus.Incident] = new[] 
        { 
            VehicleStatus.Available, 
            VehicleStatus.Maintenance 
        },
        [VehicleStatus.Decommissioned] = new[] 
        { 
            VehicleStatus.Available 
        }
    };

    public VehicleStatusService(UCarDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> ChangeStatusAsync(VehicleStatusChangeDto dto, Guid userId)
    {
        var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null)
        {
            return ServiceResult.Fail("Xe không tồn tại");
        }

        var currentStatus = vehicle.CurrentStatus;
        var newStatus = dto.NewStatus;

        // Validate state transition
        if (!IsValidTransition(currentStatus, newStatus))
        {
            var currentDisplayName = VehicleStatusHelper.GetDisplayName(currentStatus);
            var newDisplayName = VehicleStatusHelper.GetDisplayName(newStatus);
            return ServiceResult.Fail($"Không thể chuyển từ trạng thái '{currentDisplayName}' sang '{newDisplayName}'");
        }

        // Additional business rules
        var validationResult = await ValidateBusinessRulesAsync(vehicle, newStatus);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        // Update vehicle status
        vehicle.CurrentStatus = newStatus;

        // Create status history
        var statusHistory = new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = vehicle.VehicleId,
            FromStatus = currentStatus.ToString(),
            ToStatus = newStatus.ToString(),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = userId,
            Note = dto.Note?.Trim()
        };

        _context.VehicleStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync();

        var displayName = VehicleStatusHelper.GetDisplayName(newStatus);
        return ServiceResult.Ok($"Đã chuyển trạng thái xe sang '{displayName}'");
    }

    public async Task<IEnumerable<VehicleStatusHistoryDto>> GetStatusHistoryAsync(Guid vehicleId, int? limit = null)
    {
        var query = _context.VehicleStatusHistories
            .Where(h => h.VehicleId == vehicleId)
            .OrderByDescending(h => h.ChangedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<VehicleStatusHistory>)query.Take(limit.Value);
        }

        return await query
            .Select(h => new VehicleStatusHistoryDto(
                h.VshId,
                h.FromStatus,
                h.ToStatus,
                h.ChangedAt,
                h.ChangedBy,
                null, // TODO: Join with UserAccount to get name
                h.Note
            ))
            .ToListAsync();
    }

    public async Task<bool> CanChangeStatusAsync(Guid vehicleId, VehicleStatus newStatus)
    {
        var currentStatus = await GetCurrentStatusAsync(vehicleId);
        if (!currentStatus.HasValue)
        {
            return false;
        }

        return IsValidTransition(currentStatus.Value, newStatus);
    }

    public async Task<IEnumerable<VehicleStatus>> GetAllowedTransitionsAsync(Guid vehicleId)
    {
        var currentStatus = await GetCurrentStatusAsync(vehicleId);
        if (!currentStatus.HasValue)
        {
            return Enumerable.Empty<VehicleStatus>();
        }

        if (AllowedTransitions.TryGetValue(currentStatus.Value, out var transitions))
        {
            return transitions;
        }

        return Enumerable.Empty<VehicleStatus>();
    }

    public async Task<VehicleStatus?> GetCurrentStatusAsync(Guid vehicleId)
    {
        return await _context.Vehicles
            .Where(v => v.VehicleId == vehicleId)
            .Select(v => (VehicleStatus?)v.CurrentStatus)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Check if a state transition is valid according to the state machine
    /// </summary>
    private static bool IsValidTransition(VehicleStatus from, VehicleStatus to)
    {
        if (from == to) return false; // No self-transitions

        if (AllowedTransitions.TryGetValue(from, out var allowed))
        {
            return allowed.Contains(to);
        }

        return false;
    }

    /// <summary>
    /// Validate additional business rules for status changes
    /// </summary>
    private async Task<ServiceResult> ValidateBusinessRulesAsync(Vehicle vehicle, VehicleStatus newStatus)
    {
        var currentStatus = vehicle.CurrentStatus;

        // Rule 1: Cannot change to Available if there's an active booking
        if (newStatus == VehicleStatus.Available)
        {
            var hasActiveBooking = await _context.Bookings
                .AnyAsync(b => b.AssignedVehicleId == vehicle.VehicleId &&
                              (b.Status == BookingStatus.Pending || 
                               b.Status == BookingStatus.Confirmed));

            if (hasActiveBooking)
            {
                return ServiceResult.Fail("Không thể chuyển về 'Sẵn sàng' khi xe còn booking đang hiệu lực");
            }

            var hasActiveContract = await _context.RentalContracts
                .AnyAsync(c => c.VehicleId == vehicle.VehicleId &&
                              c.Status == RentalContractStatus.Active);

            if (hasActiveContract)
            {
                return ServiceResult.Fail("Không thể chuyển về 'Sẵn sàng' khi xe đang có hợp đồng thuê");
            }
        }

        // Rule 2: Cannot change to Reserved if already has pending booking for different customer
        if (newStatus == VehicleStatus.Reserved)
        {
            var hasOtherBooking = await _context.Bookings
                .AnyAsync(b => b.AssignedVehicleId == vehicle.VehicleId &&
                              (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
            
            if (hasOtherBooking)
            {
                return ServiceResult.Fail("Xe đã được đặt cho khách hàng khác");
            }
        }

        // Rule 3: Cannot change to Renting if no active contract
        if (newStatus == VehicleStatus.Renting && currentStatus != VehicleStatus.Reserved)
        {
            // Only allow Renting from Reserved status with a contract
            return ServiceResult.Fail("Xe chỉ có thể chuyển sang 'Đang thuê' khi đã được đặt trước");
        }

        // Rule 4: Cannot change to Decommissioned if has active operations
        if (newStatus == VehicleStatus.Decommissioned)
        {
            var hasActiveBooking = await _context.Bookings
                .AnyAsync(b => b.AssignedVehicleId == vehicle.VehicleId &&
                              (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));

            if (hasActiveBooking)
            {
                return ServiceResult.Fail("Không thể ngừng khai thác xe đang có booking");
            }

            var hasActiveContract = await _context.RentalContracts
                .AnyAsync(c => c.VehicleId == vehicle.VehicleId &&
                              c.Status == RentalContractStatus.Active);

            if (hasActiveContract)
            {
                return ServiceResult.Fail("Không thể ngừng khai thác xe đang có hợp đồng thuê");
            }
        }

        // Rule 5: Check for unresolved incidents when changing from Incident
        if (currentStatus == VehicleStatus.Incident && newStatus == VehicleStatus.Available)
        {
            var hasUnresolvedIncident = await _context.Incidents
                .AnyAsync(i => i.VehicleId == vehicle.VehicleId && 
                              i.Status != IncidentStatus.Resolved);

            if (hasUnresolvedIncident)
            {
                return ServiceResult.Fail("Không thể chuyển về 'Sẵn sàng' khi còn sự cố chưa giải quyết");
            }
        }

        // Rule 6: Check for incomplete maintenance when changing from Maintenance
        if (currentStatus == VehicleStatus.Maintenance && newStatus == VehicleStatus.Available)
        {
            var hasOngoingMaintenance = await _context.MaintenanceOrders
                .AnyAsync(m => m.VehicleId == vehicle.VehicleId && 
                              m.Status == MaintenanceStatus.InProgress);

            if (hasOngoingMaintenance)
            {
                return ServiceResult.Fail("Không thể chuyển về 'Sẵn sàng' khi bảo dưỡng chưa hoàn thành");
            }
        }

        return ServiceResult.Ok();
    }
}
