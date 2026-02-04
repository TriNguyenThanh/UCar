namespace UCar.Models.DTOs;

/// <summary>
/// Individual line item in price breakdown
/// </summary>
public class PriceBreakdownDto
{
    public int DisplayOrder { get; set; }
    public string LineDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
