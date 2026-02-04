using UCar.Models.DTOs;
using UCar.ViewModels.Payment;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho service quản lý thanh toán
/// </summary>
public interface IPaymentService
{
    #region Pickup Payment - Thanh toán khi nhận xe (Luồng mới)
    
    /// <summary>
    /// Lấy thông tin thanh toán khi nhận xe
    /// Áp dụng cho hợp đồng ở trạng thái PendingSigning
    /// </summary>
    Task<PickupPaymentViewModel?> GetPickupPaymentInfoAsync(Guid contractId);
    
    /// <summary>
    /// Xử lý thanh toán khi nhận xe
    /// - Tạo transaction PickupPayment
    /// - Gọi CompleteHandoverAsync để chuyển hợp đồng sang Active
    /// </summary>
    Task<ServiceResult<Guid>> ProcessPickupPaymentAsync(PickupPaymentViewModel model, Guid processedBy);
    
    #endregion
    
    #region Return Payment - Thanh toán khi trả xe
    
    /// <summary>
    /// Lấy thông tin thanh toán cho hợp đồng (khi trả xe)
    /// </summary>
    Task<PaymentProcessViewModel?> GetPaymentInfoAsync(Guid contractId);
    
    /// <summary>
    /// Xử lý thanh toán
    /// </summary>
    Task<ServiceResult> ProcessPaymentAsync(PaymentProcessViewModel model, Guid processedBy);
    
    /// <summary>
    /// Lấy thông tin hoàn tiền cho hợp đồng
    /// </summary>
    Task<RefundProcessViewModel?> GetRefundInfoAsync(Guid contractId);
    
    /// <summary>
    /// Xử lý hoàn tiền
    /// </summary>
    Task<ServiceResult> ProcessRefundAsync(RefundProcessViewModel model, Guid processedBy);
    
    #endregion
    
    /// <summary>
    /// Lấy lịch sử thanh toán của hợp đồng
    /// </summary>
    Task<PaymentHistoryViewModel> GetPaymentHistoryAsync(Guid contractId);
}
