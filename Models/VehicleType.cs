using System.ComponentModel.DataAnnotations;

namespace UCar.Models;

public class VehicleType
{
    [Key]
    public Guid VehicleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = string.Empty; // Sedan/SUV/Hatchback/Motorbike

    // Navigation properties
    public ICollection<VehicleModel> VehicleModels { get; set; } = new List<VehicleModel>();
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
