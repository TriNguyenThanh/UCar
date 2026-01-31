using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Bảng giá cho từng CHIẾC XE cụ thể - Flexible Pricing Model
/// Mỗi xe có giá riêng, không phụ thuộc vào loại xe
/// Áp dụng Single Source of Truth: chỉ nhập giá ngày cơ bản, các giá khác tự động tính
/// </summary>
public class Price
{
    [Key]
    public Guid PriceId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Bảng giá tiêu chuẩn";

    // ===== SINGLE SOURCE OF TRUTH - CHỈ NHẬP GIÁ NÀY =====
    /// <summary>Giá thuê ngày cơ bản (đ/ngày) - Base price cho mọi tính toán</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyBasePrice { get; set; }

    // ===== PRICING MULTIPLIERS (Hệ số điều chỉnh giá) =====
    /// <summary>Hệ số giá tháng (mặc định 0.85 = giảm 15%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal MonthlyMultiplier { get; set; } = 0.85m;

    /// <summary>Hệ số giá ngày lễ/tết (mặc định 1.30 = tăng 30%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal HolidayMultiplier { get; set; } = 1.30m;

    /// <summary>Hệ số giá cuối tuần (mặc định 1.15 = tăng 15%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeekendMultiplier { get; set; } = 1.15m;

    // ===== CALCULATED PRICES (Không lưu DB - tự động tính) =====
    /// <summary>Giá thuê tháng (30 ngày × hệ số tháng)</summary>
    [NotMapped]
    public decimal MonthlyPrice => DailyBasePrice * 30 * MonthlyMultiplier;

    /// <summary>Giá thuê ngày lễ/tết</summary>
    [NotMapped]
    public decimal HolidayDailyPrice => DailyBasePrice * HolidayMultiplier;

    /// <summary>Giá thuê cuối tuần (Thứ 7, CN)</summary>
    [NotMapped]
    public decimal WeekendDailyPrice => DailyBasePrice * WeekendMultiplier;

    // ===== OTHER FIELDS =====
    /// <summary>Phụ phí quá giờ (đ/giờ)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHourlyPrice { get; set; }

    /// <summary>Tiền đặt cọc đề xuất (đ)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositSuggest { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    public ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
}
