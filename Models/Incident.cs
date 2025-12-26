using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class Incident
{
    [Key]
    public Guid IncidentId { get; set; }

    [Required]
    public IncidentType IncidentType { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime OccurredAt { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public IncidentStatus Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [ForeignKey(nameof(ContractId))]
    public RentalContract? RentalContract { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    public IncidentFineDetail? FineDetail { get; set; }
    public IncidentImpoundDetail? ImpoundDetail { get; set; }
    public ICollection<IncidentCost> Costs { get; set; } = new List<IncidentCost>();
    public ICollection<IncidentDocument> Documents { get; set; } = new List<IncidentDocument>();
}
