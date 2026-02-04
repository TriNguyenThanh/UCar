using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

/// <summary>
/// Nhiệm vụ vận hành - phân công nhân viên giao nhận, bảo dưỡng, cứu hộ
/// Tương ứng DFD 8.2 - Phân công nhân viên
/// </summary>
public class OperationalTask
{
    [Key]
    public Guid TaskId { get; set; }

    [Required]
    public TaskType TaskType { get; set; }

    /// <summary>Hợp đồng liên quan (nếu có)</summary>
    public Guid? ContractId { get; set; }

    /// <summary>Xe liên quan</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Nhân viên được phân công</summary>
    public Guid? AssignedToStaffId { get; set; }

    /// <summary>Chi nhánh thực hiện</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Thời gian dự kiến thực hiện</summary>
    [Required]
    public DateTime ScheduledAt { get; set; }

    /// <summary>Thời gian dự kiến (phút)</summary>
    public int EstimatedDurationMinutes { get; set; } = 60;

    /// <summary>Thời gian hoàn thành thực tế</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Địa điểm thực hiện</summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>Tiêu đề/mô tả ngắn</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Ghi chú chi tiết</summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public OpTaskStatus Status { get; set; } = OpTaskStatus.New;

    /// <summary>Lý do thất bại/hủy</summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract? RentalContract { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    [ForeignKey(nameof(AssignedToStaffId))]
    public StaffProfile? AssignedToStaff { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }
}
