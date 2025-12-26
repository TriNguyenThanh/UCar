using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class IncidentDocument
{
    [Key]
    public Guid DocId { get; set; }

    [Required]
    public Guid IncidentId { get; set; }

    [Required]
    public IncidentDocumentType DocType { get; set; }

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    public DateTime UploadedAt { get; set; }

    [Required]
    public Guid UploadedBy { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
}
