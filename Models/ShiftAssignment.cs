using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Phân công ca làm việc cho nhân viên theo ngày
/// Tương ứng DFD 8.3 - Quản lý ca làm
/// </summary>
public class ShiftAssignment
{
    [Key]
    public Guid AssignmentId { get; set; }

    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    public Guid StaffId { get; set; }

    /// <summary>Ngày làm việc</summary>
    [Required]
    public DateOnly WorkDate { get; set; }

    /// <summary>Ghi chú</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ShiftId))]
    public Shift Shift { get; set; } = null!;

    [ForeignKey(nameof(StaffId))]
    public StaffProfile Staff { get; set; } = null!;
}
