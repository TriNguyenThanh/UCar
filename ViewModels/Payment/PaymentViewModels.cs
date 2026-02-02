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
        TransactionType.Deposit => "Đặt cọc",
        TransactionType.RentalFee => "Phí thuê xe",
        TransactionType.Penalty => "Phí phạt",
        TransactionType.Refund => "Hoàn tiền",
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
