using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

/// <summary>
/// Chính sách phụ phí cho các loại xe
/// </summary>
public class SurchargePolicy
{
    [Key]
    public Guid SurchargePolicyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PolicyName { get; set; } = string.Empty;

    [Required]
    public Guid VehicleTypeId { get; set; }

    /// <summary>Loại phụ phí: Overtime, ExtraKilometer, HolidaySurcharge, etc.</summary>
    [Required]
    public SurchargeType SurchargeType { get; set; }

    /// <summary>Phương thức tính: Percentage (%) hoặc FixedAmount (số tiền cố định)</summary>
    [Required]
    public SurchargeCalculationType CalculationType { get; set; }

    /// <summary>Giá trị tính toán: nếu Percentage thì là %, nếu FixedAmount thì là số tiền</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    /// <summary>Áp dụng trên (RentalAmount, BasePrice, PerUnit, etc.)</summary>
    [MaxLength(50)]
    public string? AppliesTo { get; set; }

    /// <summary>Đơn vị (giờ, km, ngày, etc.)</summary>
    [MaxLength(20)]
    public string? Unit { get; set; }

    /// <summary>Mô tả chi tiết</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Ngày bắt đầu áp dụng</summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>Ngày kết thúc áp dụng (null = vô thời hạn)</summary>
    public DateTime? ValidUntil { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public Guid? ModifiedBy { get; set; }

    // === NAVIGATION PROPERTIES ===

    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType VehicleType { get; set; } = null!;
}
