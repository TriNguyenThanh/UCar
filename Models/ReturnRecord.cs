using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class ReturnRecord
{
    [Key]
    public Guid ReturnId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OdoKmIn { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FuelLevelIn { get; set; }

    public DateTime ReturnedAt { get; set; }

    [MaxLength(500)]
    public string? VehicleConditionImgRef { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHours { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;
}
