namespace UCar.Models.DTOs;

/// <summary>
/// DTO for deposit breakdown calculation results
/// </summary>
public class DepositBreakdownDto
{
    /// <summary>Cọc trách nhiệm (cố định theo loại xe)</summary>
    public decimal ResponsibilityDeposit { get; set; }
    
    /// <summary>Cọc thuê xe (tính theo policy)</summary>
    public decimal RentalDeposit { get; set; }
    
    /// <summary>Tổng tiền cọc</summary>
    public decimal TotalDeposit { get; set; }
    
    /// <summary>Số ngày xử lý hoàn cọc</summary>
    public int RefundProcessingDays { get; set; }
    
    /// <summary>Ngày dự kiến hoàn cọc</summary>
    public DateTime DepositRefundDueDate { get; set; }
}
