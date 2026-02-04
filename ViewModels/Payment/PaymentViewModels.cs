using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Payment;

/// <summary>
/// ViewModel cho trang thanh toán
/// </summary>
public class PaymentProcessViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Vehicle info
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    
    // Financial info
    public decimal RentalAmount { get; set; }
    public decimal ExtraCharges { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount => AmountDue - AmountPaid;
    
    // Payment input
    [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal PaymentAmount { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; }
    
    [MaxLength(200)]
    public string? BankRefCode { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

/// <summary>
/// ViewModel cho trang hoàn tiền
/// </summary>
public class RefundProcessViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Financial info
    public decimal GrandTotal { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal AmountDue { get; set; } // Negative = refund
    public decimal RefundAmount => Math.Abs(AmountDue);
    
    // Refund input
    [Required(ErrorMessage = "Vui lòng chọn phương thức hoàn tiền")]
    public PaymentMethod RefundMethod { get; set; }
    
    [MaxLength(200)]
    public string? BankRefCode { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

/// <summary>
/// ViewModel cho lịch sử thanh toán
/// </summary>
public class PaymentHistoryViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal Balance { get; set; }
    
    public List<PaymentTransactionViewModel> Transactions { get; set; } = new();
}

/// <summary>
/// ViewModel cho một giao dịch thanh toán
/// </summary>
public class PaymentTransactionViewModel
{
    public Guid TxnId { get; set; }
    public TransactionType TxnType { get; set; }
    public string TxnTypeName => TxnType switch
    {
        TransactionType.ResponsibilityDeposit => "Cọc trách nhiệm",
        TransactionType.RentalDeposit => "Cọc thuê xe",
        TransactionType.RentalFee => "Phí thuê xe",
        TransactionType.Penalty => "Phí phạt",
        TransactionType.RefundResponsibility => "Hoàn cọc trách nhiệm",
        TransactionType.RefundRental => "Hoàn cọc thuê xe",
        TransactionType.PickupPayment => "Thanh toán nhận xe",
        TransactionType.ReturnPayment => "Thanh toán trả xe",
        TransactionType.ReturnRefund => "Hoàn tiền trả xe",
        _ => TxnType.ToString()
    };
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.Transfer => "Chuyển khoản",
        PaymentMethod.Card => "Thẻ",
        _ => PaymentMethod.ToString()
    };
    public string? BankRefCode { get; set; }
    public DateTime PaidAt { get; set; }
    public TransactionStatus Status { get; set; }
    public string StatusName => Status switch
    {
        TransactionStatus.Success => "Thành công",
        TransactionStatus.Failed => "Thất bại",
        _ => Status.ToString()
    };
    public string? Note { get; set; }
}

/// <summary>
/// ViewModel cho thanh toán khi nhận xe - Luồng mới
/// Khách đã ký hợp đồng giấy, thanh toán để nhận xe
/// </summary>
public class PickupPaymentViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    /// <summary>
    /// ID hóa đơn giao xe để thanh toán (nếu có)
    /// </summary>
    public Guid? InvoiceId { get; set; }
    
    /// <summary>
    /// ID biên bản giao xe (để in biên bản)
    /// </summary>
    public Guid? HandoverId { get; set; }
    
    // Customer info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Vehicle info
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    
    // Rental period
    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public int RentalDays { get; set; }
    
    // Financial info - Tính từ hợp đồng
    public decimal BaseRentalAmount { get; set; }     // Tiền thuê cơ bản
    public decimal AccessoryAmount { get; set; }       // Tiền phụ kiện
    public decimal TotalRentalAmount { get; set; }     // Tổng tiền thuê = Base + Accessories
    public decimal DepositAmount { get; set; }         // Tiền cọc cần thu
    
    /// <summary>
    /// Tổng tiền cần thu khi nhận xe = Tiền thuê + Cọc (từ hóa đơn)
    /// </summary>
    public decimal TotalPickupAmount { get; set; }
    
    // Payment input
    [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal PaymentAmount { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; }
    
    [MaxLength(200)]
    public string? BankRefCode { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
    
    // Deposit items - Tài sản thế chấp
    public List<CollateralItemViewModel> CollateralItems { get; set; } = new();
}

/// <summary>
/// ViewModel cho tài sản thế chấp
/// </summary>
public class CollateralItemViewModel
{
    public Guid ItemId { get; set; }
    public string ItemType { get; set; } = string.Empty;  // CCCD, Passport, Cash, Vehicle, etc.
    public string ItemTypeName => ItemType switch
    {
        "CCCD" => "Căn cước công dân",
        "Passport" => "Hộ chiếu",
        "Cash" => "Tiền mặt",
        "Vehicle" => "Phương tiện",
        "Other" => "Khác",
        _ => ItemType
    };
    public string Description { get; set; } = string.Empty;
    public decimal? EstimatedValue { get; set; }
    public string? Note { get; set; }
}