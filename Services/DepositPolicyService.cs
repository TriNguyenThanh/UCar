using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;using UCar.ViewModels;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý chính sách đặt cọc
/// </summary>
public class DepositPolicyService : IDepositPolicyService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<DepositPolicyService> _logger;

    public DepositPolicyService(UCarDbContext context, ILogger<DepositPolicyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<DepositPolicy>> GetDepositPoliciesAsync(Guid? vehicleTypeId = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        var query = _context.DepositPolicies
            .Include(dp => dp.VehicleType)
            .AsQueryable();

        if (vehicleTypeId.HasValue)
        {
            query = query.Where(dp => dp.VehicleTypeId == vehicleTypeId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(dp => dp.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(dp => dp.IsActive)
            .ThenBy(dp => dp.VehicleType.TypeName)
            .ThenByDescending(dp => dp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation($"Retrieved {items.Count} deposit policies");

        return new PagedResult<DepositPolicy>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DepositPolicy?> GetDepositPolicyByIdAsync(Guid policyId)
    {
        return await _context.DepositPolicies
            .Include(dp => dp.VehicleType)
            .FirstOrDefaultAsync(dp => dp.DepositPolicyId == policyId);
    }

    public async Task<DepositPolicy?> GetActiveDepositPolicyByVehicleTypeAsync(Guid vehicleTypeId)
    {
        var now = DateTime.Now;
        return await _context.DepositPolicies
            .Include(dp => dp.VehicleType)
            .Where(dp => dp.VehicleTypeId == vehicleTypeId
                && dp.IsActive
                && dp.ValidFrom <= now
                && (dp.ValidTo == null || dp.ValidTo >= now))
            .OrderByDescending(dp => dp.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateDepositPolicyAsync(DepositPolicy policy)
    {
        try
        {
            // Validation
            if (policy.Value <= 0)
            {
                return ServiceResult<Guid>.Fail("Giá trị tính toán phải lớn hơn 0");
            }

            if (policy.MinimumAmount < 0 || policy.MaximumAmount < 0)
            {
                return ServiceResult<Guid>.Fail("Số tiền tối thiểu và tối đa không được âm");
            }

            if (policy.MinimumAmount > policy.MaximumAmount)
            {
                return ServiceResult<Guid>.Fail("Số tiền tối thiểu không được lớn hơn tối đa");
            }

            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == policy.VehicleTypeId);

            if (!vehicleTypeExists)
            {
                return ServiceResult<Guid>.Fail("Loại xe không tồn tại");
            }

            policy.DepositPolicyId = Guid.NewGuid();
            policy.CreatedAt = DateTime.Now;
            
            _context.DepositPolicies.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created deposit policy: {policy.DepositPolicyId}");

            return ServiceResult<Guid>.Ok(policy.DepositPolicyId, "Tạo chính sách đặt cọc thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deposit policy");
            return ServiceResult<Guid>.Fail("Lỗi khi tạo chính sách: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UpdateDepositPolicyAsync(Guid policyId, DepositPolicy policy)
    {
        try
        {
            var existingPolicy = await _context.DepositPolicies.FindAsync(policyId);
            if (existingPolicy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            // Validation
            if (policy.MinimumAmount > policy.MaximumAmount)
            {
                return ServiceResult.Fail("Số tiền tối thiểu không được lớn hơn tối đa");
            }

            // Update fields
            existingPolicy.PolicyName = policy.PolicyName;
            existingPolicy.CalculationType = policy.CalculationType;
            existingPolicy.Value = policy.Value;
            existingPolicy.MinimumAmount = policy.MinimumAmount;
            existingPolicy.MaximumAmount = policy.MaximumAmount;
            existingPolicy.FullRefundCondition = policy.FullRefundCondition;
            existingPolicy.PartialRefundCondition = policy.PartialRefundCondition;
            existingPolicy.NoRefundCondition = policy.NoRefundCondition;
            existingPolicy.RefundProcessingDays = policy.RefundProcessingDays;
            existingPolicy.ValidFrom = policy.ValidFrom;
            existingPolicy.ValidTo = policy.ValidTo;
            existingPolicy.IsActive = policy.IsActive;
            existingPolicy.Description = policy.Description;
            existingPolicy.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated deposit policy: {policyId}");

            return ServiceResult.Ok("Cập nhật chính sách thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating deposit policy {policyId}");
            return ServiceResult.Fail("Lỗi khi cập nhật chính sách: " + ex.Message);
        }
    }

    public async Task<ServiceResult> DeleteDepositPolicyAsync(Guid policyId)
    {
        try
        {
            var policy = await _context.DepositPolicies.FindAsync(policyId);
            if (policy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            _context.DepositPolicies.Remove(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted deposit policy: {policyId}");

            return ServiceResult.Ok("Xóa chính sách thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting deposit policy {policyId}");
            return ServiceResult.Fail("Lỗi khi xóa chính sách: " + ex.Message);
        }
    }

    public async Task<ServiceResult> SetDepositPolicyActiveStatusAsync(Guid policyId, bool isActive)
    {
        try
        {
            var policy = await _context.DepositPolicies.FindAsync(policyId);
            if (policy == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            policy.IsActive = isActive;
            policy.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Set deposit policy {policyId} active status to {isActive}");

            return ServiceResult.Ok($"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} chính sách");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting deposit policy {policyId} active status");
            return ServiceResult.Fail("Lỗi khi cập nhật trạng thái: " + ex.Message);
        }
    }

    // ===== CALCULATION METHODS =====

    public async Task<decimal> CalculateDepositAmountAsync(Guid vehicleTypeId, decimal rentalAmount)
    {
        try
        {
            var policy = await GetActiveDepositPolicyByVehicleTypeAsync(vehicleTypeId);
            
            if (policy == null)
            {
                _logger.LogWarning($"No deposit policy found for vehicle type {vehicleTypeId}, using default 30%");
                return Math.Round(rentalAmount * 0.30m, 0);
            }

            decimal depositAmount;

            if (policy.CalculationType == DepositCalculationType.Percentage)
            {
                // Tính theo phần trăm
                depositAmount = rentalAmount * (policy.Value / 100m);
            }
            else
            {
                // Số tiền cố định
                depositAmount = policy.Value;
            }

            // Áp dụng min/max
            depositAmount = Math.Max(depositAmount, policy.MinimumAmount);
            depositAmount = Math.Min(depositAmount, policy.MaximumAmount);

            _logger.LogInformation($"Calculated deposit: {depositAmount:N0} VNĐ for rental {rentalAmount:N0} VNĐ");

            return Math.Round(depositAmount, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating deposit for vehicle type {vehicleTypeId}");
            throw;
        }
    }

    public async Task<int> DetermineRefundPercentageAsync(Guid policyId, bool isLateReturn, bool hasDamage, bool hasViolation)
    {
        try
        {
            var policy = await GetDepositPolicyByIdAsync(policyId);
            
            if (policy == null)
            {
                _logger.LogWarning($"Deposit policy {policyId} not found, returning 0% refund");
                return 0;
            }

            // Điều kiện không hoàn cọc
            if (hasViolation || hasDamage)
            {
                _logger.LogInformation("No refund due to violation or damage");
                return 0; // Không hoàn
            }

            // Điều kiện hoàn 50%
            if (isLateReturn)
            {
                _logger.LogInformation("50% refund due to late return");
                return 50; // Hoàn 50%
            }

            // Điều kiện hoàn 100%
            _logger.LogInformation("100% refund - all conditions met");
            return 100; // Hoàn 100%
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error determining refund percentage for policy {policyId}");
            throw;
        }
    }
}

