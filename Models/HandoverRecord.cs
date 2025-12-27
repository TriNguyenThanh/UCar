using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class HandoverRecord
{
    [Key]
    public Guid HandoverId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OdoKmOut { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FuelLevelOut { get; set; }

    public DateTime HandedAt { get; set; }

    [MaxLength(500)]
    public string? VehicleConditionImgRef { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;
}
