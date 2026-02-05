using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý chính sách phụ phí
/// </summary>
public class SurchargePolicyService : ISurchargePolicyService
{
    private readonly UCarDbContext _context;

    public SurchargePolicyService(UCarDbContext context)
    {
        _context = context;
    }

    // ===== SURCHARGE POLICY CRUD =====

    public async Task<PagedResult<SurchargePolicy>> GetSurchargePoliciesAsync(
        SurchargeType? type = null,
        Guid? vehicleModelId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.SurchargePolicies
            .Include(sp => sp.VehicleType)
            .AsQueryable();

        // Filter by type
        if (type.HasValue)
        {
            query = query.Where(sp => sp.SurchargeType == type.Value);
        }

        // If vehicleModelId is provided, find the corresponding VehicleType
        if (vehicleModelId.HasValue)
        {
            var model = await _context.VehicleModels
                .FirstOrDefaultAsync(vm => vm.ModelId == vehicleModelId.Value);

            if (model != null)
            {
                query = query.Where(sp => sp.VehicleTypeId == model.VehicleTypeId);
            }
        }

        // Filter by active status
        if (isActive.HasValue)
        {
            query = query.Where(sp => sp.IsActive == isActive.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(sp => sp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SurchargePolicy>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SurchargePolicy?> GetSurchargePolicyByIdAsync(Guid policyId)
    {
        return await _context.SurchargePolicies
            .Include(sp => sp.VehicleType)
            .FirstOrDefaultAsync(sp => sp.SurchargePolicyId == policyId);
    }

    public async Task<List<SurchargePolicy>> GetActiveSurchargePoliciesByTypeAsync(
        SurchargeType type, 
        Guid? vehicleModelId = null)
    {
        var query = _context.SurchargePolicies
            .Include(sp => sp.VehicleType)
            .Where(sp => sp.SurchargeType == type)
            .Where(sp => sp.IsActive)
            .AsQueryable();

        // If vehicleModelId is provided, filter by vehicle type
        if (vehicleModelId.HasValue)
        {
            var model = await _context.VehicleModels
                .FirstOrDefaultAsync(vm => vm.ModelId == vehicleModelId.Value);

            if (model != null)
            {
                query = query.Where(sp => sp.VehicleTypeId == model.VehicleTypeId);
            }
        }

        var now = DateTime.Now;
        return await query
            .Where(sp => sp.ValidFrom <= now)
            .Where(sp => sp.ValidUntil == null || sp.ValidUntil >= now)
            .OrderByDescending(sp => sp.ValidFrom)
            .ToListAsync();
    }

    public async Task<ServiceResult<Guid>> CreateSurchargePolicyAsync(SurchargePolicy policy)
    {
        try
        {
            // Validate vehicle type exists
            var typeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == policy.VehicleTypeId);

            if (!typeExists)
            {
                return ServiceResult<Guid>.Fail("Loại xe không tồn tại");
            }

            // Validate values
            if (policy.CalculationType == SurchargeCalculationType.Percentage)
            {
                if (policy.Value < 0 || policy.Value > 100)
                {
                    return ServiceResult<Guid>.Fail("Phần trăm phải từ 0-100");
                }
            }
            else
            {
                if (policy.Value < 0)
                {
                    return ServiceResult<Guid>.Fail("Giá trị phải >= 0");
                }
            }

            // Set defaults
            policy.SurchargePolicyId = Guid.NewGuid();
            policy.CreatedAt = DateTime.Now;

            _context.SurchargePolicies.Add(policy);
            await _context.SaveChangesAsync();

            return ServiceResult<Guid>.Ok(policy.SurchargePolicyId, "Tạo chính sách phụ phí thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult<Guid>.Fail($"Lỗi khi tạo chính sách: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateSurchargePolicyAsync(Guid policyId, SurchargePolicy policy)
    {
        try
        {
            var existing = await _context.SurchargePolicies.FindAsync(policyId);
            if (existing == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            // Validate values
            if (policy.CalculationType == SurchargeCalculationType.Percentage)
            {
                if (policy.Value < 0 || policy.Value > 100)
                {
                    return ServiceResult.Fail("Phần trăm phải từ 0-100");
                }
            }
            else
            {
                if (policy.Value < 0)
                {
                    return ServiceResult.Fail("Giá trị phải >= 0");
                }
            }

            // Update fields
            existing.PolicyName = policy.PolicyName;
            existing.SurchargeType = policy.SurchargeType;
            existing.CalculationType = policy.CalculationType;
            existing.Value = policy.Value;
            existing.AppliesTo = policy.AppliesTo;
            existing.Unit = policy.Unit;
            existing.Description = policy.Description;
            existing.ValidFrom = policy.ValidFrom;
            existing.ValidUntil = policy.ValidUntil;
            existing.IsActive = policy.IsActive;
            existing.ModifiedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Cập nhật chính sách thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật chính sách: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteSurchargePolicyAsync(Guid policyId)
    {
        try
        {
            var policy = await _context.SurchargePolicies.FindAsync(policyId);
            if (policy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            _context.SurchargePolicies.Remove(policy);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Xóa chính sách thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi xóa chính sách: {ex.Message}");
        }
    }

    public async Task<ServiceResult> SetSurchargePolicyActiveStatusAsync(Guid policyId, bool isActive)
    {
        try
        {
            var policy = await _context.SurchargePolicies.FindAsync(policyId);
            if (policy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            policy.IsActive = isActive;
            await _context.SaveChangesAsync();

            var status = isActive ? "kích hoạt" : "vô hiệu hóa";
            return ServiceResult.Ok($"Đã {status} chính sách");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật trạng thái: {ex.Message}");
        }
    }

    // ===== SURCHARGE CALCULATION =====

    public async Task<decimal> CalculateOvertimeSurchargeAsync(Guid vehicleModelId, decimal overtimeHours)
    {
        if (overtimeHours <= 0) return 0;

        var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.Overtime, vehicleModelId);
        if (!policies.Any())
        {
            // Default: 60,000 VND per hour
            return 60_000 * overtimeHours;
        }

        var policy = policies.First();
        return policy.Value * overtimeHours;
    }

    public async Task<decimal> CalculateExtraKilometerSurchargeAsync(Guid vehicleModelId, decimal extraKilometers)
    {
        if (extraKilometers <= 0) return 0;

        var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.ExtraKilometer, vehicleModelId);
        if (!policies.Any())
        {
            // Default: 3,000 VND per km
            return 3_000 * extraKilometers;
        }

        var policy = policies.First();
        return policy.Value * extraKilometers;
    }

    public async Task<decimal> CalculateHolidaySurchargeAsync(Guid vehicleModelId, decimal rentalAmount)
    {
        var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.HolidaySurcharge, vehicleModelId);
        if (!policies.Any())
        {
            // No holiday surcharge if no policy
            return 0;
        }

        var policy = policies.First();
        if (policy.CalculationType == SurchargeCalculationType.Percentage)
        {
            return rentalAmount * (policy.Value / 100);
        }
        else
        {
            return policy.Value;
        }
    }

    public async Task<decimal> GetFixedSurchargeAmountAsync(SurchargeType type, Guid? vehicleModelId = null)
    {
        var policies = await GetActiveSurchargePoliciesByTypeAsync(type, vehicleModelId);
        if (!policies.Any())
        {
            // Return default values based on type
            return type switch
            {
                SurchargeType.DeliveryService => 100_000,
                SurchargeType.CleaningFee => 200_000,
                SurchargeType.DriverService => 500_000,
                SurchargeType.InsuranceExtra => 50_000,
                SurchargeType.FuelShortage => 150_000,
                _ => 0
            };
        }

        return policies.First().Value;
    }

    public async Task<decimal> CalculateTotalSurchargesAsync(
        Guid vehicleModelId,
        decimal rentalAmount,
        decimal overtimeHours = 0,
        decimal extraKm = 0,
        bool needsCleaning = false,
        bool isHolidayWeekend = false,
        bool hasDelivery = false,
        bool hasDriver = false,
        int rentalDays = 0)
    {
        decimal total = 0;

        // Overtime surcharge
        if (overtimeHours > 0)
        {
            total += await CalculateOvertimeSurchargeAsync(vehicleModelId, overtimeHours);
        }

        // Extra kilometer surcharge
        if (extraKm > 0)
        {
            total += await CalculateExtraKilometerSurchargeAsync(vehicleModelId, extraKm);
        }

        // Cleaning fee
        if (needsCleaning)
        {
            total += await GetFixedSurchargeAmountAsync(SurchargeType.CleaningFee, vehicleModelId);
        }

        // Holiday surcharge
        if (isHolidayWeekend)
        {
            total += await CalculateHolidaySurchargeAsync(vehicleModelId, rentalAmount);
        }

        // Delivery service
        if (hasDelivery)
        {
            total += await GetFixedSurchargeAmountAsync(SurchargeType.DeliveryService, vehicleModelId);
        }

        // Driver service (usually per day)
        if (hasDriver && rentalDays > 0)
        {
            var driverFeePerDay = await GetFixedSurchargeAmountAsync(SurchargeType.DriverService, vehicleModelId);
            total += driverFeePerDay * rentalDays;
        }

        return total;
    }
}
