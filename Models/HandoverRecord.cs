using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Biên bản giao xe (Check-out record)
/// Tương ứng DFD 6.1 + 6.2 - Bàn giao xe cho khách & Ghi nhận tình trạng trước khi thuê
/// </summary>
public class HandoverRecord
{
    [Key]
    public Guid HandoverId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    /// <summary>Số km ODO khi giao</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OdoKmOut { get; set; }

    /// <summary>Mức nhiên liệu/pin khi giao (0-100%)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FuelLevelOut { get; set; }

    /// <summary>Thời gian bàn giao</summary>
    public DateTime HandedAt { get; set; }

    /// <summary>Tình trạng ngoại thất</summary>
    [MaxLength(1000)]
    public string? ExteriorCondition { get; set; }

    /// <summary>Tình trạng nội thất</summary>
    [MaxLength(1000)]
    public string? InteriorCondition { get; set; }

    /// <summary>Hư hỏng có sẵn (pre-existing damages)</summary>
    [MaxLength(1000)]
    public string? PreExistingDamages { get; set; }

    /// <summary>Đường dẫn ảnh tình trạng xe (JSON array hoặc folder path)</summary>
    [MaxLength(500)]
    public string? VehicleConditionImgRef { get; set; }

    /// <summary>Khách hàng đã xác nhận</summary>
    public bool CustomerConfirmed { get; set; }

    /// <summary>Thời gian khách xác nhận</summary>
    public DateTime? CustomerConfirmedAt { get; set; }

    /// <summary>Nhân viên bàn giao</summary>
    public Guid HandedOverBy { get; set; }

    /// <summary>Ghi chú</summary>
    [MaxLength(1000)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;

    [ForeignKey(nameof(HandedOverBy))]
    public UserAccount HandedOverByUser { get; set; } = null!;

    /// <summary>Phụ kiện bàn giao</summary>
    public ICollection<HandoverAccessory> Accessories { get; set; } = new List<HandoverAccessory>();
}

