using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SurchargePolicyService> _logger;

    public SurchargePolicyService(UCarDbContext context, ILogger<SurchargePolicyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<SurchargePolicy>> GetSurchargePoliciesAsync(
        SurchargeType? type = null,
        Guid? vehicleTypeId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _context.SurchargePolicies
                .Include(sp => sp.VehicleType)
                .AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(sp => sp.Type == type.Value);
            }

            if (vehicleTypeId.HasValue)
            {
                query = query.Where(sp => sp.VehicleTypeId == vehicleTypeId.Value || sp.VehicleTypeId == null);
            }

            if (isActive.HasValue)
            {
                query = query.Where(sp => sp.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(sp => sp.IsActive)
                .ThenByDescending(sp => sp.Priority)
                .ThenBy(sp => sp.Type)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation($"Retrieved {items.Count} surcharge policies");

            return new PagedResult<SurchargePolicy>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surcharge policies");
            throw;
        }
    }

    public async Task<SurchargePolicy?> GetSurchargePolicyByIdAsync(Guid policyId)
    {
        try
        {
            return await _context.SurchargePolicies
                .Include(sp => sp.VehicleType)
                .FirstOrDefaultAsync(sp => sp.SurchargePolicyId == policyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving surcharge policy {policyId}");
            throw;
        }
    }

    public async Task<List<SurchargePolicy>> GetActiveSurchargePoliciesByTypeAsync(SurchargeType type, Guid? vehicleTypeId = null)
    {
        try
        {
            var now = DateTime.Now;
            var query = _context.SurchargePolicies
                .Include(sp => sp.VehicleType)
                .Where(sp => sp.Type == type
                    && sp.IsActive
                    && sp.ValidFrom <= now
                    && (sp.ValidTo == null || sp.ValidTo >= now));

            if (vehicleTypeId.HasValue)
            {
                query = query.Where(sp => sp.VehicleTypeId == vehicleTypeId.Value || sp.VehicleTypeId == null);
            }

            var policies = await query
                .OrderByDescending(sp => sp.Priority)
                .ThenByDescending(sp => sp.VehicleTypeId) // Ưu tiên policy riêng cho loại xe
                .ToListAsync();

            return policies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving active surcharge policies for type {type}");
            throw;
        }
    }

    public async Task<ServiceResult<Guid>> CreateSurchargePolicyAsync(SurchargePolicy policy)
    {
        try
        {
            // Validation
            if (policy.Value <= 0)
            {
                return ServiceResult<Guid>.Fail("Giá trị tính toán phải lớn hơn 0");
            }

            if (policy.VehicleTypeId.HasValue)
            {
                var vehicleTypeExists = await _context.VehicleTypes
                    .AnyAsync(vt => vt.VehicleTypeId == policy.VehicleTypeId.Value);

                if (!vehicleTypeExists)
                {
                    return ServiceResult<Guid>.Fail("Loại xe không tồn tại");
                }
            }

            policy.SurchargePolicyId = Guid.NewGuid();
            policy.CreatedAt = DateTime.Now;

            _context.SurchargePolicies.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created surcharge policy: {policy.SurchargePolicyId} ({policy.Type})");

            return ServiceResult<Guid>.Ok(policy.SurchargePolicyId, "Tạo chính sách phụ phí thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating surcharge policy");
            return ServiceResult<Guid>.Fail("Lỗi khi tạo chính sách: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UpdateSurchargePolicyAsync(Guid policyId, SurchargePolicy policy)
    {
        try
        {
            var existingPolicy = await _context.SurchargePolicies.FindAsync(policyId);
            if (existingPolicy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            // Update fields
            existingPolicy.PolicyName = policy.PolicyName;
            existingPolicy.Type = policy.Type;
            existingPolicy.VehicleTypeId = policy.VehicleTypeId;
            existingPolicy.CalculationType = policy.CalculationType;
            existingPolicy.Value = policy.Value;
            existingPolicy.MinimumAmount = policy.MinimumAmount;
            existingPolicy.MaximumAmount = policy.MaximumAmount;
            existingPolicy.ApplicableCondition = policy.ApplicableCondition;
            existingPolicy.Priority = policy.Priority;
            existingPolicy.ValidFrom = policy.ValidFrom;
            existingPolicy.ValidTo = policy.ValidTo;
            existingPolicy.IsActive = policy.IsActive;
            existingPolicy.Description = policy.Description;
            existingPolicy.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated surcharge policy: {policyId}");

            return ServiceResult.Ok("Cập nhật chính sách thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating surcharge policy {policyId}");
            return ServiceResult.Fail("Lỗi khi cập nhật chính sách: " + ex.Message);
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

            _logger.LogInformation($"Deleted surcharge policy: {policyId}");

            return ServiceResult.Ok("Xóa chính sách thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting surcharge policy {policyId}");
            return ServiceResult.Fail("Lỗi khi xóa chính sách: " + ex.Message);
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
            policy.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Set surcharge policy {policyId} active status to {isActive}");

            return ServiceResult.Ok($"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} chính sách");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting surcharge policy {policyId} active status");
            return ServiceResult.Fail("Lỗi khi cập nhật trạng thái: " + ex.Message);
        }
    }

    // ===== CALCULATION METHODS =====

    public async Task<decimal> CalculateOvertimeSurchargeAsync(Guid vehicleTypeId, decimal overtimeHours)
    {
        try
        {
            if (overtimeHours <= 0)
            {
                return 0;
            }

            var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.Overtime, vehicleTypeId);
            var policy = policies.FirstOrDefault();

            if (policy == null)
            {
                _logger.LogWarning($"No overtime surcharge policy found, using default 50k/hour");
                return Math.Round(overtimeHours * 50000m, 0);
            }

            decimal amount = 0;

            if (policy.CalculationType == SurchargeCalculationType.PerUnit)
            {
                amount = overtimeHours * policy.Value;
            }
            else if (policy.CalculationType == SurchargeCalculationType.FixedAmount)
            {
                amount = policy.Value;
            }

            // Apply min/max if applicable
            if (policy.MinimumAmount.HasValue)
            {
                amount = Math.Max(amount, policy.MinimumAmount.Value);
            }
            if (policy.MaximumAmount.HasValue)
            {
                amount = Math.Min(amount, policy.MaximumAmount.Value);
            }

            _logger.LogInformation($"Overtime surcharge: {amount:N0} VNĐ for {overtimeHours} hours");

            return Math.Round(amount, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overtime surcharge");
            throw;
        }
    }

    public async Task<decimal> CalculateExtraKilometerSurchargeAsync(Guid vehicleTypeId, decimal extraKilometers)
    {
        try
        {
            if (extraKilometers <= 0)
            {
                return 0;
            }

            var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.ExtraKilometer, vehicleTypeId);
            var policy = policies.FirstOrDefault();

            if (policy == null)
            {
                _logger.LogWarning($"No extra kilometer policy found, using default 5k/km");
                return Math.Round(extraKilometers * 5000m, 0);
            }

            decimal amount = extraKilometers * policy.Value;

            _logger.LogInformation($"Extra km surcharge: {amount:N0} VNĐ for {extraKilometers} km");

            return Math.Round(amount, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating extra kilometer surcharge");
            throw;
        }
    }

    public async Task<decimal> CalculateHolidaySurchargeAsync(Guid vehicleTypeId, decimal rentalAmount)
    {
        try
        {
            var policies = await GetActiveSurchargePoliciesByTypeAsync(SurchargeType.HolidayWeekend, vehicleTypeId);
            var policy = policies.FirstOrDefault();

            if (policy == null)
            {
                _logger.LogWarning($"No holiday surcharge policy found, using default 15%");
                return Math.Round(rentalAmount * 0.15m, 0);
            }

            decimal amount = 0;

            if (policy.CalculationType == SurchargeCalculationType.Percentage)
            {
                amount = rentalAmount * (policy.Value / 100m);
            }
            else
            {
                amount = policy.Value;
            }

            // Apply min/max
            if (policy.MinimumAmount.HasValue)
            {
                amount = Math.Max(amount, policy.MinimumAmount.Value);
            }
            if (policy.MaximumAmount.HasValue)
            {
                amount = Math.Min(amount, policy.MaximumAmount.Value);
            }

            _logger.LogInformation($"Holiday surcharge: {amount:N0} VNĐ for rental {rentalAmount:N0} VNĐ");

            return Math.Round(amount, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating holiday surcharge");
            throw;
        }
    }

    public async Task<decimal> GetFixedSurchargeAmountAsync(SurchargeType type, Guid? vehicleTypeId = null)
    {
        try
        {
            var policies = await GetActiveSurchargePoliciesByTypeAsync(type, vehicleTypeId);
            var policy = policies.FirstOrDefault();

            if (policy == null)
            {
                _logger.LogWarning($"No surcharge policy found for type {type}");
                return 0;
            }

            _logger.LogInformation($"Fixed surcharge for {type}: {policy.Value:N0} VNĐ");

            return Math.Round(policy.Value, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting fixed surcharge for type {type}");
            throw;
        }
    }

    public async Task<decimal> CalculateTotalSurchargesAsync(
        Guid vehicleTypeId,
        decimal rentalAmount,
        decimal overtimeHours = 0,
        decimal extraKm = 0,
        bool needsCleaning = false,
        bool isHolidayWeekend = false,
        bool hasDelivery = false,
        bool hasDriver = false,
        int rentalDays = 0)
    {
        try
        {
            decimal totalSurcharges = 0;

            // Phụ phí quá giờ
            if (overtimeHours > 0)
            {
                var overtimeSurcharge = await CalculateOvertimeSurchargeAsync(vehicleTypeId, overtimeHours);
                totalSurcharges += overtimeSurcharge;
                _logger.LogInformation($"+ Overtime: {overtimeSurcharge:N0} VNĐ");
            }

            // Phụ phí quá km
            if (extraKm > 0)
            {
                var kmSurcharge = await CalculateExtraKilometerSurchargeAsync(vehicleTypeId, extraKm);
                totalSurcharges += kmSurcharge;
                _logger.LogInformation($"+ Extra km: {kmSurcharge:N0} VNĐ");
            }

            // Phụ phí vệ sinh
            if (needsCleaning)
            {
                var cleaningSurcharge = await GetFixedSurchargeAmountAsync(SurchargeType.CleaningFee, vehicleTypeId);
                totalSurcharges += cleaningSurcharge;
                _logger.LogInformation($"+ Cleaning: {cleaningSurcharge:N0} VNĐ");
            }

            // Phụ phí ngày lễ/cuối tuần
            if (isHolidayWeekend)
            {
                var holidaySurcharge = await CalculateHolidaySurchargeAsync(vehicleTypeId, rentalAmount);
                totalSurcharges += holidaySurcharge;
                _logger.LogInformation($"+ Holiday: {holidaySurcharge:N0} VNĐ");
            }

            // Phụ phí giao xe
            if (hasDelivery)
            {
                var deliverySurcharge = await GetFixedSurchargeAmountAsync(SurchargeType.DeliveryService, vehicleTypeId);
                totalSurcharges += deliverySurcharge;
                _logger.LogInformation($"+ Delivery: {deliverySurcharge:N0} VNĐ");
            }

            // Phụ phí thuê lái xe (tính theo ngày)
            if (hasDriver && rentalDays > 0)
            {
                var driverSurchargePerDay = await GetFixedSurchargeAmountAsync(SurchargeType.DriverService, vehicleTypeId);
                var driverSurcharge = driverSurchargePerDay * rentalDays;
                totalSurcharges += driverSurcharge;
                _logger.LogInformation($"+ Driver ({rentalDays} days): {driverSurcharge:N0} VNĐ");
            }

            _logger.LogInformation($"=== Total surcharges: {totalSurcharges:N0} VNĐ ===");

            return Math.Round(totalSurcharges, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total surcharges");
            throw;
        }
    }
}
