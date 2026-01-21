using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý xe - sử dụng DbContext trực tiếp (không Repository)
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly UCarDbContext _context;
    private readonly IVehicleStatusService _statusService;

    public VehicleService(UCarDbContext context, IVehicleStatusService statusService)
    {
        _context = context;
        _statusService = statusService;
    }

    public async Task<PagedResult<VehicleListDto>> GetAllVehiclesAsync(VehicleFilterDto? filter = null)
    {
        filter ??= new VehicleFilterDto();

        var query = _context.Vehicles
            .Include(v => v.Model)
                .ThenInclude(m => m.VehicleType)
            .Include(v => v.Branch)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(v =>
                v.PlateNo.ToLower().Contains(term) ||
                v.Model.Make.ToLower().Contains(term) ||
                v.Model.ModelName.ToLower().Contains(term));
        }

        if (filter.VehicleTypeId.HasValue)
        {
            query = query.Where(v => v.Model.VehicleTypeId == filter.VehicleTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Make))
        {
            query = query.Where(v => v.Model.Make == filter.Make);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(v => v.CurrentStatus == filter.Status.Value);
        }

        if (filter.BranchId.HasValue)
        {
            query = query.Where(v => v.BranchId == filter.BranchId.Value);
        }

        if (filter.YearFrom.HasValue)
        {
            query = query.Where(v => v.ManufactureYear >= filter.YearFrom.Value);
        }

        if (filter.YearTo.HasValue)
        {
            query = query.Where(v => v.ManufactureYear <= filter.YearTo.Value);
        }

        // Get total count before paging
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "plateno" => filter.SortDirection == "desc"
                ? query.OrderByDescending(v => v.PlateNo)
                : query.OrderBy(v => v.PlateNo),
            "make" => filter.SortDirection == "desc"
                ? query.OrderByDescending(v => v.Model.Make)
                : query.OrderBy(v => v.Model.Make),
            "year" => filter.SortDirection == "desc"
                ? query.OrderByDescending(v => v.ManufactureYear)
                : query.OrderBy(v => v.ManufactureYear),
            "status" => filter.SortDirection == "desc"
                ? query.OrderByDescending(v => v.CurrentStatus)
                : query.OrderBy(v => v.CurrentStatus),
            _ => query.OrderBy(v => v.PlateNo)
        };

        // Apply paging
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VehicleListDto(
                v.VehicleId,
                v.PlateNo,
                v.Model.Make,
                v.Model.ModelName,
                v.Model.VehicleType.TypeName,
                v.Color,
                v.ManufactureYear,
                v.CurrentStatus,
                v.BranchId,
                v.Branch.Name,
                v.CurrentOdoKm,
                v.Model.Seats,
                v.Model.Transmission,
                v.Model.FuelType
            ))
            .ToListAsync();

        return new PagedResult<VehicleListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<VehicleDetailDto?> GetVehicleByIdAsync(Guid id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Model)
                .ThenInclude(m => m.VehicleType)
            .Include(v => v.Branch)
            .Include(v => v.StatusHistories.OrderByDescending(h => h.ChangedAt).Take(10))
            .FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null) return null;

        var statusHistory = vehicle.StatusHistories
            .Select(h => new VehicleStatusHistoryDto(
                h.VshId,
                h.FromStatus,
                h.ToStatus,
                h.ChangedAt,
                h.ChangedBy,
                null, // TODO: Join with UserAccount to get name
                h.Note
            ));

        return new VehicleDetailDto(
            vehicle.VehicleId,
            vehicle.PlateNo,
            vehicle.ModelId,
            vehicle.Model.Make,
            vehicle.Model.ModelName,
            vehicle.Model.VehicleTypeId,
            vehicle.Model.VehicleType.TypeName,
            vehicle.Color,
            vehicle.ManufactureYear,
            vehicle.Model.Seats,
            vehicle.Model.Transmission,
            vehicle.Model.FuelType,
            vehicle.CurrentStatus,
            vehicle.BranchId,
            vehicle.Branch.Name,
            vehicle.Branch.Address,
            vehicle.CurrentOdoKm,
            statusHistory,
            null // TODO: Add vehicle images when model is created
        );
    }

    public async Task<ServiceResult<Guid>> CreateVehicleAsync(VehicleCreateDto dto, Guid userId)
    {
        // Validate plate number uniqueness
        if (await IsPlateNoExistsAsync(dto.PlateNo))
        {
            return ServiceResult<Guid>.Fail("Biển số xe đã tồn tại trong hệ thống");
        }

        // Validate ModelId exists
        var modelExists = await _context.VehicleModels.AnyAsync(m => m.ModelId == dto.ModelId);
        if (!modelExists)
        {
            return ServiceResult<Guid>.Fail("Dòng xe không tồn tại");
        }

        // Validate BranchId exists
        var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == dto.BranchId);
        if (!branchExists)
        {
            return ServiceResult<Guid>.Fail("Chi nhánh không tồn tại");
        }

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            PlateNo = dto.PlateNo.Trim().ToUpper(),
            ModelId = dto.ModelId,
            BranchId = dto.BranchId,
            Color = dto.Color?.Trim(),
            ManufactureYear = dto.ManufactureYear,
            CurrentOdoKm = dto.CurrentOdoKm,
            CurrentStatus = VehicleStatus.Available
        };

        _context.Vehicles.Add(vehicle);

        // Create initial status history
        var statusHistory = new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = vehicle.VehicleId,
            FromStatus = null,
            ToStatus = VehicleStatus.Available.ToString(),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = userId,
            Note = "Thêm xe mới vào hệ thống"
        };

        _context.VehicleStatusHistories.Add(statusHistory);

        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(vehicle.VehicleId, "Thêm xe thành công");
    }

    public async Task<ServiceResult> UpdateVehicleAsync(Guid id, VehicleUpdateDto dto, Guid userId)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return ServiceResult.Fail("Xe không tồn tại");
        }

        // Validate plate number uniqueness (exclude current vehicle)
        if (await IsPlateNoExistsAsync(dto.PlateNo, id))
        {
            return ServiceResult.Fail("Biển số xe đã tồn tại trong hệ thống");
        }

        // Validate ModelId exists
        var modelExists = await _context.VehicleModels.AnyAsync(m => m.ModelId == dto.ModelId);
        if (!modelExists)
        {
            return ServiceResult.Fail("Dòng xe không tồn tại");
        }

        // Validate BranchId exists
        var branchExists = await _context.Branches.AnyAsync(b => b.BranchId == dto.BranchId);
        if (!branchExists)
        {
            return ServiceResult.Fail("Chi nhánh không tồn tại");
        }

        vehicle.PlateNo = dto.PlateNo.Trim().ToUpper();
        vehicle.ModelId = dto.ModelId;
        vehicle.BranchId = dto.BranchId;
        vehicle.Color = dto.Color?.Trim();
        vehicle.ManufactureYear = dto.ManufactureYear;
        vehicle.CurrentOdoKm = dto.CurrentOdoKm;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật xe thành công");
    }

    public async Task<ServiceResult> DeleteVehicleAsync(Guid id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Bookings)
            .Include(v => v.RentalContracts)
            .Include(v => v.MaintenanceOrders)
            .Include(v => v.Incidents)
            .FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return ServiceResult.Fail("Xe không tồn tại");
        }

        // Rule 1: Check vehicle status - only allow delete if Available or Decommissioned
        var allowedStatuses = new[] { VehicleStatus.Available, VehicleStatus.Decommissioned };
        if (!allowedStatuses.Contains(vehicle.CurrentStatus))
        {
            var statusName = VehicleStatusHelper.GetDisplayName(vehicle.CurrentStatus);
            return ServiceResult.Fail($"Không thể xóa xe đang ở trạng thái '{statusName}'. Chỉ có thể xóa xe 'Sẵn sàng' hoặc 'Ngừng khai thác'.");
        }

        // Rule 2: Check for any bookings (not just active ones - for audit trail)
        if (vehicle.Bookings.Any())
        {
            return ServiceResult.Fail($"Không thể xóa xe đã có {vehicle.Bookings.Count} lịch sử đặt xe");
        }

        // Rule 3: Check for any rental contracts
        if (vehicle.RentalContracts.Any())
        {
            return ServiceResult.Fail($"Không thể xóa xe đã có {vehicle.RentalContracts.Count} hợp đồng thuê");
        }

        // Rule 4: Check for maintenance orders
        if (vehicle.MaintenanceOrders.Any())
        {
            return ServiceResult.Fail($"Không thể xóa xe đã có {vehicle.MaintenanceOrders.Count} lịch sử bảo dưỡng");
        }

        // Rule 5: Check for incidents
        if (vehicle.Incidents.Any())
        {
            return ServiceResult.Fail($"Không thể xóa xe đã có {vehicle.Incidents.Count} lịch sử sự cố");
        }

        // Remove status histories first (cascade should handle this, but be explicit)
        var histories = await _context.VehicleStatusHistories
            .Where(h => h.VehicleId == id)
            .ToListAsync();
        _context.VehicleStatusHistories.RemoveRange(histories);

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa xe thành công");
    }

    public async Task<IEnumerable<VehicleListDto>> GetAvailableVehiclesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Vehicles
            .Include(v => v.Model)
                .ThenInclude(m => m.VehicleType)
            .Include(v => v.Branch)
            .Where(v => v.CurrentStatus == VehicleStatus.Available);

        // TODO: Add date range filtering based on bookings

        return await query
            .Select(v => new VehicleListDto(
                v.VehicleId,
                v.PlateNo,
                v.Model.Make,
                v.Model.ModelName,
                v.Model.VehicleType.TypeName,
                v.Color,
                v.ManufactureYear,
                v.CurrentStatus,
                v.BranchId,
                v.Branch.Name,
                v.CurrentOdoKm,
                v.Model.Seats,
                v.Model.Transmission,
                v.Model.FuelType
            ))
            .ToListAsync();
    }

    public async Task<bool> IsPlateNoExistsAsync(string plateNo, Guid? excludeId = null)
    {
        var normalizedPlate = plateNo.Trim().ToUpper();
        var query = _context.Vehicles.Where(v => v.PlateNo == normalizedPlate);

        if (excludeId.HasValue)
        {
            query = query.Where(v => v.VehicleId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<string>> GetMakesAsync()
    {
        return await _context.VehicleModels
            .Select(m => m.Make)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<IEnumerable<(Guid Id, string Name)>> GetBranchesAsync()
    {
        return await _context.Branches
            .Select(b => new ValueTuple<Guid, string>(b.BranchId, b.Name))
            .ToListAsync();
    }
}
