using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class IncidentImpoundDetail
{
    [Key, ForeignKey(nameof(Incident))]
    public Guid IncidentId { get; set; }

    [MaxLength(500)]
    public string? ImpoundLotAddress { get; set; }

    public DateTime ImpoundedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
}
