using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

/// <summary>
/// Chính sách đặt cọc cho các loại xe
/// </summary>
public class DepositPolicy
{
    [Key]
    public Guid DepositPolicyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PolicyName { get; set; } = string.Empty;

    [Required]
    public Guid VehicleTypeId { get; set; }

    /// <summary>Phương thức tính tiền cọc: Percentage (phần trăm) hoặc FixedAmount (số tiền cố định)</summary>
    [Required]
    public DepositCalculationType CalculationType { get; set; }

    /// <summary>Giá trị tính toán: nếu Percentage thì là % (0-100), nếu FixedAmount thì là số tiền</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    /// <summary>Số tiền cọc tối thiểu</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumAmount { get; set; }

    /// <summary>Số tiền cọc tối đa</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaximumAmount { get; set; }

    /// <summary>Điều kiện hoàn cọc 100%</summary>
    [MaxLength(1000)]
    public string? FullRefundCondition { get; set; }

    /// <summary>Điều kiện hoàn cọc 50%</summary>
    [MaxLength(1000)]
    public string? PartialRefundCondition { get; set; }

    /// <summary>Điều kiện không hoàn cọc</summary>
    [MaxLength(1000)]
    public string? NoRefundCondition { get; set; }

    /// <summary>Số ngày xử lý hoàn cọc</summary>
    public int RefundProcessingDays { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType VehicleType { get; set; } = null!;
}
