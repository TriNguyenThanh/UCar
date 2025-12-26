using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class CollateralItem
{
    [Key]
    public Guid CollateralId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public CollateralItemType ItemType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; }

    [MaxLength(500)]
    public string? ConditionAtReceived { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ReturnedAt { get; set; }

    [Required]
    public CollateralStatus Status { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;
}
