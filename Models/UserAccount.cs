using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class UserAccount
{
    [Key]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    public Customer? Customer { get; set; }
    public StaffProfile? StaffProfile { get; set; }
    public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
    public ICollection<RentalContract> HandledContracts { get; set; } = new List<RentalContract>();
}
