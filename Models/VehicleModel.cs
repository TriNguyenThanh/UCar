using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class VehicleModel
{
    [Key]
    public Guid ModelId { get; set; }

    [Required]
    public Guid VehicleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Make { get; set; } = string.Empty; // Toyota/Honda

    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty; // Vios/City

    public int Seats { get; set; }

    public TransmissionType? Transmission { get; set; }

    [MaxLength(50)]
    public string? FuelType { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType VehicleType { get; set; } = null!;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
