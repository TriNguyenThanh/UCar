using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class Price
{
    [Key]
    public Guid PriceId { get; set; }

    [Required]
    public Guid VehicleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PriceUnit Unit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHourlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositSuggest { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType VehicleType { get; set; } = null!;

    public ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
}
