using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class PaymentTransaction
{
    [Key]
    public Guid TxnId { get; set; }

    public Guid? ContractId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public TransactionType TxnType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(200)]
    public string? BankRefCode { get; set; }

    public DateTime PaidAt { get; set; }

    [Required]
    public TransactionStatus Status { get; set; }

    public ReferenceType? RefType { get; set; }

    public Guid? RefId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract? RentalContract { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;
}
