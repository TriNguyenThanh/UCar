namespace UCar.Models.DTOs;

/// <summary>
/// Price estimate result for rental period
/// </summary>
public class PriceEstimateDto
{
    public Guid VehicleModelId { get; set; }
    public string VehicleModelName { get; set; } = string.Empty;
    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }
    
    // Rental period details
    public int TotalDays { get; set; }
    public int NormalDays { get; set; }
    public int PeakDays { get; set; }
    
    // Pricing details
    public decimal BaseDailyPrice { get; set; }
    public decimal MonthMultiplier { get; set; }
    public decimal PeakMultiplier { get; set; }
    
    // Calculation results
    public bool IsMonthlyRate { get; set; }
    public decimal NormalDaysAmount { get; set; }
    public decimal PeakDaysAmount { get; set; }
    public decimal MonthlyAmount { get; set; }
    public decimal SubTotal { get; set; }
    
    // Deposits
    public decimal ResponsibilityDeposit { get; set; } = 2_000_000; // Fixed 2M
    public decimal RentalDeposit { get; set; } // 50% of SubTotal
    public decimal TotalDeposit { get; set; }
    
    // Grand total
    public decimal TotalAmount { get; set; }
    
    // Breakdown for display
    public List<PriceBreakdownDto> Breakdown { get; set; } = new();
}
