using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class MaintenanceOrder
{
    [Key]
    public Guid MoId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    [MaxLength(200)]
    public string? ProviderName { get; set; }

    [Required]
    public MaintenanceStatus Status { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;
}
