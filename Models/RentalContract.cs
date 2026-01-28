using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class RentalContract
{
    [Key]
    public Guid ContractId { get; set; }

    /// <summary>Mã hợp đồng (hiển thị cho user, e.g. HD-001234)</summary>
    [Required]
    [MaxLength(20)]
    public string ContractCode { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }

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

    /// <summary>Địa điểm nhận xe</summary>
    [MaxLength(500)]
    public string? PickupLocation { get; set; }

    /// <summary>Địa điểm trả xe</summary>
    [MaxLength(500)]
    public string? ReturnLocation { get; set; }

    /// <summary>Tổng số ngày thuê (tính toán)</summary>
    public int RentalDays { get; set; }

    /// <summary>Tiền thuê xe (không bao gồm phụ phí)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal RentalAmount { get; set; }

    /// <summary>Tổng phụ phí</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExtraCharges { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmountFinal { get; set; }

    [Required]
    public RentalContractStatus Status { get; set; }

    /// <summary>Điều khoản hợp đồng</summary>
    [MaxLength(4000)]
    public string? Terms { get; set; }

    /// <summary>Ghi chú nội bộ</summary>
    [MaxLength(1000)]
    public string? InternalNote { get; set; }

    // ===== Thông tin ký/xác nhận =====

    /// <summary>Nhân viên xác nhận hợp đồng</summary>
    public Guid? ConfirmedBy { get; set; }

    /// <summary>Thời gian nhân viên xác nhận</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Khách hàng đã ký/đồng ý điều khoản</summary>
    public bool CustomerSigned { get; set; }

    /// <summary>Thời gian khách ký</summary>
    public DateTime? CustomerSignedAt { get; set; }

    // ===== Thông tin hủy =====

    /// <summary>Lý do hủy</summary>
    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    /// <summary>Người hủy</summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>Thời gian hủy</summary>
    public DateTime? CancelledAt { get; set; }

    // ===== Audit =====

    [Required]
    public Guid HandledBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Concurrency token</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking? Booking { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [ForeignKey(nameof(PriceId))]
    public Price Price { get; set; } = null!;

    [ForeignKey(nameof(HandledBy))]
    public UserAccount Handler { get; set; } = null!;

    [ForeignKey(nameof(ConfirmedBy))]
    public UserAccount? Confirmer { get; set; }

    [ForeignKey(nameof(CancelledBy))]
    public UserAccount? Canceller { get; set; }

    public HandoverRecord? HandoverRecord { get; set; }
    public ReturnRecord? ReturnRecord { get; set; }
    public ICollection<ContractViolation> Violations { get; set; } = new List<ContractViolation>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<ContractCharge> Charges { get; set; } = new List<ContractCharge>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<CollateralItem> CollateralItems { get; set; } = new List<CollateralItem>();
}
