using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class IncidentCost
{
    [Key]
    public Guid CostId { get; set; }

    [Required]
    public Guid IncidentId { get; set; }

    [Required]
    public IncidentCostType CostType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime IncurredAt { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
}
