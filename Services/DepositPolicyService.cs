using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý chính sách đặt cọc
/// </summary>
public class DepositPolicyService : IDepositPolicyService
{
    private readonly UCarDbContext _context;

    public DepositPolicyService(UCarDbContext context)
    {
        _context = context;
    }

    // ===== DEPOSIT POLICY CRUD =====

    public async Task<PagedResult<DepositPolicy>> GetDepositPoliciesAsync(
        Guid? vehicleModelId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.DepositPolicies
            .Include(dp => dp.VehicleType)
            .AsQueryable();

        // If vehicleModelId is provided, find the corresponding VehicleType
        if (vehicleModelId.HasValue)
        {
            var model = await _context.VehicleModels
                .FirstOrDefaultAsync(vm => vm.ModelId == vehicleModelId.Value);
            
            if (model != null)
            {
                query = query.Where(dp => dp.VehicleTypeId == model.VehicleTypeId);
            }
        }

        // Filter by active status
        if (isActive.HasValue)
        {
            query = query.Where(dp => dp.IsActive == isActive.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(dp => dp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DepositPolicy>
        {
            Items = items,
            TotalCount = total,
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

    public async Task<DepositPolicy?> GetActiveDepositPolicyByVehicleModelAsync(Guid vehicleModelId)
    {
        // Get the vehicle model to find its type
        var model = await _context.VehicleModels
            .FirstOrDefaultAsync(vm => vm.ModelId == vehicleModelId);

        if (model == null)
        {
            return null;
        }

        // Find active deposit policy for this vehicle type
        return await _context.DepositPolicies
            .Include(dp => dp.VehicleType)
            .Where(dp => dp.VehicleTypeId == model.VehicleTypeId)
            .Where(dp => dp.IsActive)
            .OrderByDescending(dp => dp.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateDepositPolicyAsync(DepositPolicy policy)
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
            if (policy.CalculationType == DepositCalculationType.Percentage)
            {
                if (policy.Value < 0 || policy.Value > 100)
                {
                    return ServiceResult<Guid>.Fail("Phần trăm cọc phải từ 0-100");
                }
            }
            else
            {
                if (policy.Value < 0)
                {
                    return ServiceResult<Guid>.Fail("Số tiền cọc phải >= 0");
                }
            }

            if (policy.MinimumAmount < 0 || policy.MaximumAmount < 0)
            {
                return ServiceResult<Guid>.Fail("Số tiền tối thiểu/tối đa phải >= 0");
            }

            if (policy.MaximumAmount < policy.MinimumAmount)
            {
                return ServiceResult<Guid>.Fail("Số tiền tối đa phải >= số tiền tối thiểu");
            }

            // Set defaults
            policy.DepositPolicyId = Guid.NewGuid();
            policy.CreatedAt = DateTime.Now;

            _context.DepositPolicies.Add(policy);
            await _context.SaveChangesAsync();

            return ServiceResult<Guid>.Ok(policy.DepositPolicyId, "Tạo chính sách đặt cọc thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult<Guid>.Fail($"Lỗi khi tạo chính sách: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateDepositPolicyAsync(Guid policyId, DepositPolicy policy)
    {
        try
        {
            var existing = await _context.DepositPolicies.FindAsync(policyId);
            if (existing == null)
            {
                return ServiceResult.Fail("Chính sách không tồn tại");
            }

            // Validate values
            if (policy.CalculationType == DepositCalculationType.Percentage)
            {
                if (policy.Value < 0 || policy.Value > 100)
                {
                    return ServiceResult.Fail("Phần trăm cọc phải từ 0-100");
                }
            }
            else
            {
                if (policy.Value < 0)
                {
                    return ServiceResult.Fail("Số tiền cọc phải >= 0");
                }
            }

            if (policy.MinimumAmount < 0 || policy.MaximumAmount < 0)
            {
                return ServiceResult.Fail("Số tiền tối thiểu/tối đa phải >= 0");
            }

            if (policy.MaximumAmount < policy.MinimumAmount)
            {
                return ServiceResult.Fail("Số tiền tối đa phải >= số tiền tối thiểu");
            }

            // Update fields
            existing.PolicyName = policy.PolicyName;
            existing.CalculationType = policy.CalculationType;
            existing.Value = policy.Value;
            existing.MinimumAmount = policy.MinimumAmount;
            existing.MaximumAmount = policy.MaximumAmount;
            existing.FullRefundCondition = policy.FullRefundCondition;
            existing.PartialRefundCondition = policy.PartialRefundCondition;
            existing.NoRefundCondition = policy.NoRefundCondition;
            existing.RefundProcessingDays = policy.RefundProcessingDays;
            existing.IsActive = policy.IsActive;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Cập nhật chính sách thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật chính sách: {ex.Message}");
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

            return ServiceResult.Ok("Xóa chính sách thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi xóa chính sách: {ex.Message}");
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
            await _context.SaveChangesAsync();

            var status = isActive ? "kích hoạt" : "vô hiệu hóa";
            return ServiceResult.Ok($"Đã {status} chính sách");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật trạng thái: {ex.Message}");
        }
    }

    // ===== DEPOSIT CALCULATION =====

    public async Task<decimal> CalculateDepositAmountAsync(Guid vehicleModelId, decimal rentalAmount)
    {
        var policy = await GetActiveDepositPolicyByVehicleModelAsync(vehicleModelId);
        
        if (policy == null)
        {
            // Default: 50% of rental amount, min 2M, max 20M
            return Math.Max(2_000_000, Math.Min(rentalAmount * 0.5m, 20_000_000));
        }

        decimal depositAmount;

        if (policy.CalculationType == DepositCalculationType.Percentage)
        {
            // Calculate percentage of rental amount
            depositAmount = rentalAmount * (policy.Value / 100);
        }
        else
        {
            // Use fixed amount
            depositAmount = policy.Value;
        }

        // Apply min/max constraints
        if (policy.MinimumAmount > 0)
        {
            depositAmount = Math.Max(depositAmount, policy.MinimumAmount);
        }

        if (policy.MaximumAmount > 0)
        {
            depositAmount = Math.Min(depositAmount, policy.MaximumAmount);
        }

        return depositAmount;
    }

    public async Task<int> DetermineRefundPercentageAsync(
        Guid policyId, 
        bool isLateReturn, 
        bool hasDamage, 
        bool hasViolation)
    {
        var policy = await _context.DepositPolicies.FindAsync(policyId);
        
        if (policy == null)
        {
            // Default behavior: no refund if any issues
            if (isLateReturn || hasDamage || hasViolation)
            {
                return 0;
            }
            return 100;
        }

        // Check conditions based on policy configuration
        // This is a simplified version - actual logic should parse the condition strings

        if (isLateReturn || hasDamage || hasViolation)
        {
            // Check if it qualifies for partial refund
            if (isLateReturn && !hasDamage && !hasViolation)
            {
                return 50; // Partial refund for only late return
            }
            return 0; // No refund if damage or violation
        }

        return 100; // Full refund if no issues
    }
}
