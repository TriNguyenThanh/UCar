using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class ContractViolation
{
    [Key]
    public Guid ViolationId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public ViolationType ViolationType { get; set; }

    public DateTime DetectedAt { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ViolationStatus Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PenaltyAmount { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;

    public ICollection<ContractCharge> ContractCharges { get; set; } = new List<ContractCharge>();
}
