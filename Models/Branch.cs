using System.ComponentModel.DataAnnotations;

namespace UCar.Models;

public class Branch
{
    [Key]
    public Guid BranchId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? PhoneContact { get; set; }

    // Navigation properties
    public ICollection<StaffProfile> StaffProfiles { get; set; } = new List<StaffProfile>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
