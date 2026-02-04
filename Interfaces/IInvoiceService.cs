using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;

namespace UCar.Interfaces
{
    /// <summary>
    /// Service quản lý hóa đơn theo từng giai đoạn nghiệp vụ
    /// </summary>
    public interface IInvoiceService
    {
        // ===== INVOICE CREATION =====
        
        /// <summary>
        /// Tạo hóa đơn đặt cọc khi ký hợp đồng
        /// Bao gồm: Cọc trách nhiệm (từ DepositPolicy) + Cọc thuê xe (từ DepositPolicy)
        /// </summary>
        Task<Guid> CreateDepositInvoiceAsync(Guid contractId, Guid issuedBy);

        /// <summary>
        /// Tạo hóa đơn tiền thuê khi nhận xe (handover)
        /// Hiển thị breakdown từ ContractPriceBreakdown, trừ cọc thuê xe đã trả
        /// </summary>
        Task<Guid> CreateRentalInvoiceAsync(Guid contractId, Guid issuedBy);

        /// <summary>
        /// Tạo hóa đơn phụ phí/phạt khi trả xe (return)
        /// Chỉ tạo nếu có ContractCharges
        /// Sử dụng SurchargePolicy để lấy giá phụ phí
        /// </summary>
        Task<Guid?> CreateSurchargePenaltyInvoiceAsync(Guid contractId, Guid issuedBy);

        /// <summary>
        /// Tạo hóa đơn hoàn cọc sau RefundProcessingDays từ khi trả xe
        /// Hoàn: Cọc trách nhiệm + Cọc thuê xe - Các khoản chưa thanh toán
        /// </summary>
        Task<Guid> CreateRefundInvoiceAsync(Guid contractId, Guid issuedBy);

        // ===== INVOICE QUERIES =====

        /// <summary>
        /// Lấy danh sách hóa đơn với filtering và pagination
        /// </summary>
        Task<(List<Invoice> Items, int TotalCount, int TotalPages)> GetInvoicesAsync(
            InvoiceStatus? status = null,
            InvoiceType? type = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 15);

        /// <summary>
        /// Lấy chi tiết hóa đơn bao gồm LineItems và Adjustments
        /// </summary>
        Task<Invoice?> GetInvoiceDetailsAsync(Guid invoiceId);

        /// <summary>
        /// Lấy tất cả hóa đơn của 1 hợp đồng
        /// </summary>
        Task<List<Invoice>> GetInvoicesByContractAsync(Guid contractId);

        /// <summary>
        /// Lấy hóa đơn theo loại và hợp đồng
        /// </summary>
        Task<Invoice?> GetInvoiceByTypeAndContractAsync(InvoiceType type, Guid contractId);

        // ===== PAYMENT OPERATIONS =====

        /// <summary>
        /// Lấy thông tin thanh toán của hóa đơn
        /// </summary>
        Task<InvoicePaymentInfoDto?> GetInvoicePaymentInfoAsync(Guid invoiceId);

        /// <summary>
        /// Kiểm tra hóa đơn đã thanh toán đầy đủ chưa
        /// </summary>
        Task<bool> IsInvoicePaidAsync(Guid invoiceId);

        /// <summary>
        /// Lấy Invoice để xử lý payment (bao gồm các navigation properties cần thiết)
        /// </summary>
        Task<Invoice?> GetInvoiceForPaymentAsync(Guid invoiceId);

        /// <summary>
        /// Ghi nhận thanh toán cho hóa đơn
        /// Tạo PaymentTransaction và cập nhật AmountPaid, AmountDue, Status
        /// </summary>
        Task<bool> RecordPaymentAsync(Guid invoiceId, decimal amount, PaymentMethod paymentMethod, string? bankRefCode, Guid paidBy, string? notes = null);

        // ===== INVOICE ADJUSTMENTS =====

        /// <summary>
        /// Điều chỉnh hóa đơn (sửa lỗi, giảm giá, điều chỉnh thuế)
        /// </summary>
        Task<Guid> AddAdjustmentAsync(
            Guid invoiceId,
            AdjustmentType adjustmentType,
            decimal amount,
            string reason,
            Guid adjustedBy,
            string? notes = null);

        /// <summary>
        /// Hủy hóa đơn
        /// </summary>
        Task<bool> CancelInvoiceAsync(Guid invoiceId, string reason, Guid cancelledBy);

        // ===== BACKGROUND SERVICES =====

        /// <summary>
        /// Background service: Tạo hóa đơn hoàn cọc tự động
        /// Chạy daily, tìm contracts đã return > RefundProcessingDays và chưa có Refund Invoice
        /// </summary>
        Task ProcessRefundInvoicesAsync();

        /// <summary>
        /// Background service: Kiểm tra hóa đơn quá hạn
        /// Cập nhật status thành Overdue và gửi email nhắc nhở
        /// </summary>
        Task CheckOverdueInvoicesAsync();

        // ===== UTILITIES =====

        /// <summary>
        /// Sinh mã hóa đơn: DEP-2602-0001, RNT-2602-0002, SUR-2602-0003, REF-2602-0004
        /// </summary>
        Task<string> GenerateInvoiceNumberAsync(string prefix);
    }
}
