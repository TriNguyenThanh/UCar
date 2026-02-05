using UCar.Models.Enums;

namespace UCar.Models.DTOs;

/// <summary>
/// DTO chứa thông tin thanh toán của Invoice
/// </summary>
public class InvoicePaymentInfoDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public string InvoiceTypeDisplay { get; set; } = string.Empty;
    
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    
    public InvoiceStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    // Breakdown details
    public decimal BaseRentalAmount { get; set; }
    public decimal SurchargesTotal { get; set; }
    public decimal PenaltiesTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DepositPaid { get; set; }
}
