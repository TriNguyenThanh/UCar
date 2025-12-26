using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class RentalContract
{
    [Key]
    public Guid ContractId { get; set; }

    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    public Guid PriceId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SnapshotUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SnapshotDepositAmount { get; set; }

    public DateTime PlannedStart { get; set; }

    public DateTime PlannedEnd { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmountFinal { get; set; }

    [Required]
    public RentalContractStatus Status { get; set; }

    [Required]
    public Guid HandledBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [ForeignKey(nameof(PriceId))]
    public Price Price { get; set; } = null!;

    [ForeignKey(nameof(HandledBy))]
    public UserAccount Handler { get; set; } = null!;

    public HandoverRecord? HandoverRecord { get; set; }
    public ReturnRecord? ReturnRecord { get; set; }
    public ICollection<ContractViolation> Violations { get; set; } = new List<ContractViolation>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<ContractCharge> Charges { get; set; } = new List<ContractCharge>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<CollateralItem> CollateralItems { get; set; } = new List<CollateralItem>();
}
