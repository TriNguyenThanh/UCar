using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class Price
{
    [Key]
    public Guid PriceId { get; set; }

    [Required]
    public Guid VehicleModelId { get; set; }
    
    /// <summary>Optional: For type-level pricing</summary>
    public Guid? VehicleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Giá ngày thường (base daily price)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseDailyPrice { get; set; }

    /// <summary>Hệ số tháng (monthly multiplier) - VD: 0.85 = giảm 15%</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthMultiplier { get; set; }

    /// <summary>Hệ số lễ (peak/holiday multiplier) - VD: 1.5 = tăng 50%</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PeakMultiplier { get; set; }

    /// <summary>Giá vượt giờ (overtime hourly rate)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHourlyPrice { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VehicleModelId))]
    public VehicleModel VehicleModel { get; set; } = null!;
    
    [ForeignKey(nameof(VehicleTypeId))]
    public VehicleType? VehicleType { get; set; }

    public ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
}
