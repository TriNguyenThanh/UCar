using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class Booking
{
    [Key]
    public Guid BookingId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid VehicleTypeId { get; set; }

    public Guid? AssignedVehicleId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    [Required]
    public BookingStatus Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedTotal { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType VehicleType { get; set; } = null!;

    [ForeignKey(nameof(AssignedVehicleId))]
    public Vehicle? AssignedVehicle { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public UserAccount CreatedByUser { get; set; } = null!;

    public RentalContract? RentalContract { get; set; }
}
