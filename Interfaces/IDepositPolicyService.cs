using UCar.Models;
using UCar.Models.DTOs;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý chính sách đặt cọc
/// </summary>
public interface IDepositPolicyService
{
    // ===== DEPOSIT POLICY CRUD =====
    
    /// <summary>
    /// Lấy danh sách chính sách đặt cọc
    /// </summary>
    Task<PagedResult<DepositPolicy>> GetDepositPoliciesAsync(Guid? vehicleModelId = null, bool? isActive = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy chi tiết chính sách đặt cọc theo ID
    /// </summary>
    Task<DepositPolicy?> GetDepositPolicyByIdAsync(Guid policyId);

    /// <summary>
    /// Lấy chính sách đặt cọc đang active cho model xe
    /// </summary>
    Task<DepositPolicy?> GetActiveDepositPolicyByVehicleModelAsync(Guid vehicleModelId);

    /// <summary>
    /// Tạo chính sách đặt cọc mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateDepositPolicyAsync(DepositPolicy policy);

    /// <summary>
    /// Cập nhật chính sách đặt cọc
    /// </summary>
    Task<ServiceResult> UpdateDepositPolicyAsync(Guid policyId, DepositPolicy policy);

    /// <summary>
    /// Xóa chính sách đặt cọc
    /// </summary>
    Task<ServiceResult> DeleteDepositPolicyAsync(Guid policyId);

    /// <summary>
    /// Kích hoạt/vô hiệu hóa chính sách
    /// </summary>
    Task<ServiceResult> SetDepositPolicyActiveStatusAsync(Guid policyId, bool isActive);

    // ===== DEPOSIT CALCULATION =====
    
    /// <summary>
    /// Tính chi tiết đặt cọc (tách biệt ResponsibilityDeposit và RentalDeposit)
    /// </summary>
    /// <param name="vehicleModelId">Model xe</param>
    /// <param name="rentalAmount">Tổng tiền thuê (chưa bao gồm cọc)</param>
    /// <returns>Chi tiết deposit breakdown</returns>
    Task<DepositBreakdownDto> CalculateDepositBreakdownAsync(Guid vehicleModelId, decimal rentalAmount);
    
    /// <summary>
    /// Tính số tiền cọc dựa trên policy và giá trị hợp đồng (LEGACY - chỉ trả về rental deposit)
    /// </summary>
    /// <param name="vehicleModelId">Model xe</param>
    /// <param name="rentalAmount">Tổng tiền thuê (chưa bao gồm cọc)</param>
    /// <returns>Số tiền cọc cần thu</returns>
    Task<decimal> CalculateDepositAmountAsync(Guid vehicleModelId, decimal rentalAmount);

    /// <summary>
    /// Kiểm tra điều kiện hoàn cọc và tính % hoàn
    /// </summary>
    /// <param name="policyId">ID của deposit policy</param>
    /// <param name="isLateReturn">Có trả xe muộn không</param>
    /// <param name="hasDamage">Có hư hỏng không</param>
    /// <param name="hasViolation">Có vi phạm không</param>
    /// <returns>Phần trăm hoàn cọc (0-100)</returns>
    Task<int> DetermineRefundPercentageAsync(Guid policyId, bool isLateReturn, bool hasDamage, bool hasViolation);
}
