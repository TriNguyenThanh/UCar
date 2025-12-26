using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class VehicleStatusHistory
{
    [Key]
    public Guid VshId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [MaxLength(50)]
    public string? FromStatus { get; set; }

    [Required]
    [MaxLength(50)]
    public string ToStatus { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    [Required]
    public Guid ChangedBy { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;
}
