using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class Vehicle
{
    [Key]
    public Guid VehicleId { get; set; }

    [Required]
    public Guid BranchId { get; set; }

    [Required]
    public Guid ModelId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PlateNo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Color { get; set; }

    public int ManufactureYear { get; set; }

    [Required]
    public VehicleStatus CurrentStatus { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentOdoKm { get; set; }

    // Navigation properties
    [ForeignKey(nameof(BranchId))]
    public Branch Branch { get; set; } = null!;

    [ForeignKey(nameof(ModelId))]
    public VehicleModel Model { get; set; } = null!;

    public ICollection<VehicleStatusHistory> StatusHistories { get; set; } = new List<VehicleStatusHistory>();
    public ICollection<MaintenanceOrder> MaintenanceOrders { get; set; } = new List<MaintenanceOrder>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
