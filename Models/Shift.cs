using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Ca làm việc - định nghĩa khung giờ làm việc
/// Tương ứng DFD 8.3 - Quản lý ca làm
/// </summary>
public class Shift
{
    [Key]
    public Guid ShiftId { get; set; }

    /// <summary>Tên ca làm (VD: Ca sáng, Ca chiều, Ca tối)</summary>
    [Required]
    [MaxLength(100)]
    public string ShiftName { get; set; } = string.Empty;

    /// <summary>Giờ bắt đầu</summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>Giờ kết thúc</summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>Chi nhánh áp dụng (null = toàn bộ chi nhánh)</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Mô tả</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(BranchId))]
    public Branch? Branch { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}
