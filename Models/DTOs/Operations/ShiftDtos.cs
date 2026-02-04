using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs.Operations;

// ============ Shift DTOs ============

public class ShiftDto
{
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string TimeRange => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ShiftCreateDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên ca làm")]
    [MaxLength(100)]
    public string ShiftName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
    public TimeOnly EndTime { get; set; }

    public Guid? BranchId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class ShiftUpdateDto : ShiftCreateDto
{
    public bool IsActive { get; set; } = true;
}

// ============ Shift Assignment DTOs ============

public class ShiftAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;    public string BranchName { get; set; } = string.Empty;    public DateOnly WorkDate { get; set; }
    public string? Notes { get; set; }
}

public class ShiftAssignmentCreateDto
{
    [Required(ErrorMessage = "Vui lòng chọn ca làm")]
    public Guid ShiftId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
    public Guid StaffId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày làm việc")]
    public DateOnly WorkDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class ShiftScheduleFilterDto
{
    public Guid? BranchId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? StaffId { get; set; }
}

// ============ Calendar Event DTO (for FullCalendar) ============

public class CalendarEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;  // ISO 8601 format
    public string End { get; set; } = string.Empty;    // ISO 8601 format
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public bool AllDay { get; set; } = false;
    public object? ExtendedProps { get; set; }
}
