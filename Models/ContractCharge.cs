using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class ContractCharge
{
    [Key]
    public Guid ChargeId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    public Guid? ViolationId { get; set; }

    [Required]
    public ChargeType ChargeType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;

    [ForeignKey(nameof(ViolationId))]
    public ContractViolation? Violation { get; set; }
}
