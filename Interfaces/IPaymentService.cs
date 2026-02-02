using UCar.Models.DTOs;
using UCar.ViewModels.Payment;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho service quản lý thanh toán
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Lấy thông tin thanh toán cho hợp đồng
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
    
    /// <summary>
    /// Lấy lịch sử thanh toán của hợp đồng
    /// </summary>
    Task<PaymentHistoryViewModel> GetPaymentHistoryAsync(Guid contractId);
}
