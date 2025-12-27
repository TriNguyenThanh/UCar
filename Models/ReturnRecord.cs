using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Biên bản nhận xe (Check-in record)
/// Tương ứng DFD 6.3 + 6.4 - Nhận xe từ khách & Đối soát tình trạng sau thuê
/// </summary>
public class ReturnRecord
{
    [Key]
    public Guid ReturnId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    /// <summary>Số km ODO khi nhận</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OdoKmIn { get; set; }

    /// <summary>Mức nhiên liệu/pin khi nhận (0-100%)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FuelLevelIn { get; set; }

    /// <summary>Thời gian nhận xe</summary>
    public DateTime ReturnedAt { get; set; }

    /// <summary>Tình trạng ngoại thất khi trả</summary>
    [MaxLength(1000)]
    public string? ExteriorCondition { get; set; }

    /// <summary>Tình trạng nội thất khi trả</summary>
    [MaxLength(1000)]
    public string? InteriorCondition { get; set; }

    /// <summary>Hư hỏng phát hiện khi nhận</summary>
    [MaxLength(1000)]
    public string? DamagesFound { get; set; }

    /// <summary>Đường dẫn ảnh tình trạng xe</summary>
    [MaxLength(500)]
    public string? VehicleConditionImgRef { get; set; }

    /// <summary>Cần vệ sinh</summary>
    public bool NeedsCleaning { get; set; }

    /// <summary>Cần bảo dưỡng/sửa chữa</summary>
    public bool NeedsMaintenance { get; set; }

    /// <summary>Số giờ quá hạn</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHours { get; set; }

    /// <summary>Nhiên liệu thiếu (%)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FuelShortage { get; set; }

    /// <summary>Nhân viên nhận xe</summary>
    public Guid ReceivedBy { get; set; }

    /// <summary>Ghi chú</summary>
    [MaxLength(1000)]
    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;

    [ForeignKey(nameof(ReceivedBy))]
    public UserAccount ReceivedByUser { get; set; } = null!;

    /// <summary>Phụ kiện đã trả</summary>
    public ICollection<HandoverAccessory> AccessoriesReturned { get; set; } = new List<HandoverAccessory>();
}

