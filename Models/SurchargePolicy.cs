using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

/// <summary>
/// Chính sách phụ phí áp dụng cho hợp đồng thuê xe
/// </summary>
public class SurchargePolicy
{
    [Key]
    public Guid SurchargePolicyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PolicyName { get; set; } = string.Empty;

    [Required]
    public SurchargeType Type { get; set; }

    /// <summary>Áp dụng cho loại xe cụ thể (null = áp dụng cho tất cả)</summary>
    public Guid? VehicleTypeId { get; set; }

    /// <summary>Phương thức tính: Percentage (%) hoặc FixedAmount (số tiền cố định)</summary>
    [Required]
    public SurchargeCalculationType CalculationType { get; set; }

    /// <summary>Giá trị tính toán</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    /// <summary>Số tiền tối thiểu (cho Percentage)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumAmount { get; set; }

    /// <summary>Số tiền tối đa (cho Percentage)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumAmount { get; set; }

    /// <summary>Điều kiện áp dụng (VD: quá giờ từ 2h trở lên, lái xe quá 300km/ngày)</summary>
    [MaxLength(1000)]
    public string? ApplicableCondition { get; set; }

    /// <summary>Mức độ ưu tiên (số càng cao càng ưu tiên)</summary>
    public int Priority { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType? VehicleType { get; set; }
}
