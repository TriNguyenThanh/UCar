using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class IncidentFineDetail
{
    [Key, ForeignKey(nameof(Incident))]
    public Guid IncidentId { get; set; }

    [MaxLength(100)]
    public string? TicketNumber { get; set; }

    [MaxLength(200)]
    public string? AgencyName { get; set; }

    public DateTime? DueDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FineAmount { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
}
