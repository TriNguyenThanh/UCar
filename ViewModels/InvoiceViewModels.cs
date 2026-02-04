using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách hóa đơn
    /// </summary>
    public class InvoiceListViewModel
    {
        public Guid InvoiceId { get; set; }

        [Display(Name = "Mã hóa đơn")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Loại hóa đơn")]
        public InvoiceType InvoiceType { get; set; }

        [Display(Name = "Mã hợp đồng")]
        public string ContractCode { get; set; } = string.Empty;

        [Display(Name = "Khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Biển số xe")]
        public string VehiclePlateNo { get; set; } = string.Empty;

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Còn phải trả")]
        public decimal AmountDue { get; set; }

        [Display(Name = "Đã thanh toán")]
        public decimal PreviouslyPaid { get; set; }

        [Display(Name = "Trạng thái")]
        public InvoiceStatus Status { get; set; }

        [Display(Name = "Ngày phát hành")]
        public DateTime IssuedDate { get; set; }

        [Display(Name = "Hạn thanh toán")]
        public DateTime? DueDate { get; set; }

        // Helper properties for display
        public string InvoiceTypeDisplay => InvoiceType switch
        {
            InvoiceType.Deposit => "Đặt cọc",
            InvoiceType.Rental => "Tiền thuê",
            InvoiceType.Surcharge => "Phụ phí",
            InvoiceType.Penalty => "Phạt",
            InvoiceType.Refund => "Hoàn cọc",
            _ => InvoiceType.ToString()
        };

        public string StatusDisplay => Status switch
        {
            InvoiceStatus.Draft => "Nháp",
            InvoiceStatus.Issued => "Đã phát hành",
            InvoiceStatus.PartiallyPaid => "Trả một phần",
            InvoiceStatus.Paid => "Đã thanh toán",
            InvoiceStatus.Overdue => "Quá hạn",
            InvoiceStatus.Cancelled => "Đã hủy",
            InvoiceStatus.Refunded => "Đã hoàn tiền",
            _ => Status.ToString()
        };

        public string StatusBadgeClass => Status switch
        {
            InvoiceStatus.Draft => "badge bg-secondary",
            InvoiceStatus.Issued => "badge bg-primary",
            InvoiceStatus.PartiallyPaid => "badge bg-warning",
            InvoiceStatus.Paid => "badge bg-success",
            InvoiceStatus.Overdue => "badge bg-danger",
            InvoiceStatus.Cancelled => "badge bg-dark",
            InvoiceStatus.Refunded => "badge bg-info",
            _ => "badge bg-secondary"
        };

        public bool IsOverdue => Status == InvoiceStatus.Overdue;
        public bool CanRecordPayment => Status == InvoiceStatus.Issued || Status == InvoiceStatus.PartiallyPaid || Status == InvoiceStatus.Overdue;
    }

    /// <summary>
    /// ViewModel cho chi tiết hóa đơn
    /// </summary>
    public class InvoiceDetailsViewModel
    {
        // Invoice Info
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType InvoiceType { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? DueDate { get; set; }

        // Contract Info
        public Guid ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public string VehiclePlateNo { get; set; } = string.Empty;
        public string VehicleModelName { get; set; } = string.Empty;
        public DateTime RentalStart { get; set; }
        public DateTime RentalEnd { get; set; }

        // Customer Info
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Amounts Breakdown
        public decimal BaseRentalAmount { get; set; }
        public decimal SurchargesTotal { get; set; }
        public decimal PenaltiesTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DepositPaid { get; set; }

        // Final Amounts
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }

        // Line Items
        public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

        // Adjustments
        public List<InvoiceAdjustmentViewModel> Adjustments { get; set; } = new();

        // Staff Info
        public string IssuedByName { get; set; } = string.Empty;

        // Notes
        public string? Notes { get; set; }
        public string? InternalNotes { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Display helpers
        public string InvoiceTypeDisplay => InvoiceType switch
        {
            InvoiceType.Deposit => "Hóa đơn đặt cọc",
            InvoiceType.Rental => "Hóa đơn tiền thuê",
            InvoiceType.Surcharge => "Hóa đơn phụ phí",
            InvoiceType.Penalty => "Hóa đơn phạt",
            InvoiceType.Refund => "Hóa đơn hoàn cọc",
            _ => InvoiceType.ToString()
        };

        public string StatusDisplay => Status switch
        {
            InvoiceStatus.Draft => "Nháp",
            InvoiceStatus.Issued => "Đã phát hành",
            InvoiceStatus.PartiallyPaid => "Đã trả một phần",
            InvoiceStatus.Paid => "Đã thanh toán đầy đủ",
            InvoiceStatus.Overdue => "Quá hạn thanh toán",
            InvoiceStatus.Cancelled => "Đã hủy",
            InvoiceStatus.Refunded => "Đã hoàn tiền",
            _ => Status.ToString()
        };

        public string StatusBadgeClass => Status switch
        {
            InvoiceStatus.Draft => "badge bg-secondary",
            InvoiceStatus.Issued => "badge bg-primary",
            InvoiceStatus.PartiallyPaid => "badge bg-warning text-dark",
            InvoiceStatus.Paid => "badge bg-success",
            InvoiceStatus.Overdue => "badge bg-danger",
            InvoiceStatus.Cancelled => "badge bg-dark",
            InvoiceStatus.Refunded => "badge bg-info",
            _ => "badge bg-secondary"
        };

        public bool IsOverdue => Status == InvoiceStatus.Overdue;
        public bool CanRecordPayment => Status == InvoiceStatus.Issued || Status == InvoiceStatus.PartiallyPaid || Status == InvoiceStatus.Overdue;
        public bool CanCancel => Status == InvoiceStatus.Draft || Status == InvoiceStatus.Issued;
    }

    /// <summary>
    /// ViewModel cho line item trong hóa đơn
    /// </summary>
    public class InvoiceLineItemViewModel
    {
        public Guid LineItemId { get; set; }
        public LineItemType ItemType { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        public string ItemTypeDisplay => ItemType switch
        {
            LineItemType.ResponsibilityDeposit => "Cọc trách nhiệm",
            LineItemType.RentalDeposit => "Cọc thuê xe",
            LineItemType.BaseRental => "Thuê ngày thường",
            LineItemType.PeakRental => "Thuê ngày lễ",
            LineItemType.MonthlyRental => "Thuê theo tháng",
            LineItemType.OvertimeSurcharge => "Phụ phí vượt giờ",
            LineItemType.ExtraKmSurcharge => "Phụ phí vượt km",
            LineItemType.CleaningSurcharge => "Phí vệ sinh",
            LineItemType.FuelSurcharge => "Phí nhiên liệu",
            LineItemType.ContractViolation => "Vi phạm hợp đồng",
            LineItemType.DamagePenalty => "Phạt hư hỏng",
            LineItemType.RefundResponsibility => "Hoàn cọc trách nhiệm",
            LineItemType.RefundRental => "Hoàn cọc thuê xe",
            LineItemType.Discount => "Giảm giá",
            LineItemType.Tax => "Thuế",
            _ => "Khác"
        };
    }

    /// <summary>
    /// ViewModel cho adjustment trong hóa đơn
    /// </summary>
    public class InvoiceAdjustmentViewModel
    {
        public Guid AdjustmentId { get; set; }
        public AdjustmentType AdjustmentType { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string AdjustedByName { get; set; } = string.Empty;
        public DateTime AdjustedAt { get; set; }
        public string? Notes { get; set; }

        public string AdjustmentTypeDisplay => AdjustmentType switch
        {
            AdjustmentType.Correction => "Sửa lỗi",
            AdjustmentType.Discount => "Giảm giá",
            AdjustmentType.TaxAdjustment => "Điều chỉnh thuế",
            _ => "Khác"
        };
    }

    /// <summary>
    /// ViewModel cho in hóa đơn
    /// </summary>
    public class InvoicePrintViewModel
    {
        // Invoice Info
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType InvoiceType { get; set; }
        public string InvoiceTypeText { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? DueDate { get; set; }

        // Company Info
        public string CompanyName { get; set; } = "UCar - Hệ thống cho thuê xe";
        public string CompanyAddress { get; set; } = "123 Đường ABC, Quận 1, TP.HCM";
        public string CompanyPhone { get; set; } = "028-1234-5678";
        public string CompanyEmail { get; set; } = "contact@ucar.vn";
        public string CompanyTaxCode { get; set; } = "0123456789";

        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerTaxCode { get; set; }

        // Contract Info
        public string ContractCode { get; set; } = string.Empty;
        public string VehiclePlateNo { get; set; } = string.Empty;
        public string VehicleModelName { get; set; } = string.Empty;
        public DateTime RentalStart { get; set; }
        public DateTime RentalEnd { get; set; }
        public int? RentalDays { get; set; }

        // Line Items
        public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

        // Amounts
        public decimal BaseRentalAmount { get; set; }
        public decimal SurchargesTotal { get; set; }
        public decimal PenaltiesTotal { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }

        // Issuer
        public string IssuedByName { get; set; } = string.Empty;

        // Notes
        public string? Notes { get; set; }

        // Display helpers
        public string StatusDisplay => Status switch
        {
            InvoiceStatus.Draft => "Nháp",
            InvoiceStatus.Issued => "Đã phát hành",
            InvoiceStatus.PartiallyPaid => "Đã trả một phần",
            InvoiceStatus.Paid => "Đã thanh toán đầy đủ",
            InvoiceStatus.Overdue => "Quá hạn thanh toán",
            InvoiceStatus.Cancelled => "Đã hủy",
            InvoiceStatus.Refunded => "Đã hoàn tiền",
            _ => Status.ToString()
        };
    }

    /// <summary>
    /// ViewModel cho record payment form
    /// </summary>
    public class RecordPaymentViewModel
    {
        public Guid InvoiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        [Display(Name = "Số tiền thanh toán")]
        public decimal AmountPaid { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Mã tham chiếu ngân hàng")]
        [MaxLength(100, ErrorMessage = "Mã tham chiếu không được vượt quá 100 ký tự")]
        public string? BankRefCode { get; set; }

        [Display(Name = "Ghi chú")]
        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        // Display info
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PreviouslyPaid { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}
