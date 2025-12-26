using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class Customer
{
    [Key]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }

    [MaxLength(500)]
    public string? AddressText { get; set; }

    [MaxLength(50)]
    public string? RiskLevel { get; set; }

    public bool IsBlacklisted { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public UserAccount UserAccount { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<CustomerDocument> Documents { get; set; } = new List<CustomerDocument>();
}
