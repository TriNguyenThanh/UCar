using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Chi tiết breakdown giá trong hợp đồng (để hiển thị rõ ràng)
/// </summary>
public class ContractPriceBreakdown
{
    [Key]
    public Guid BreakdownId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    [MaxLength(200)]
    public string LineDescription { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;
}
