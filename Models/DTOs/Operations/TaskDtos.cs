using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Operations;

// ============ Operational Task DTOs ============

public class TaskListDto
{
    public Guid TaskId { get; set; }
    public TaskType TaskType { get; set; }
    public string TaskTypeName => TaskType switch
    {
        TaskType.Delivery => "Giao xe",
        TaskType.Return => "Nhận xe",
        TaskType.Maintenance => "Bảo dưỡng",
        TaskType.Rescue => "Cứu hộ",
        TaskType.Inspection => "Kiểm tra",
        _ => TaskType.ToString()
    };
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? Location { get; set; }
    public OpTaskStatus Status { get; set; }
    public string StatusName => Status switch
    {
        OpTaskStatus.New => "Mới",
        OpTaskStatus.Assigned => "Đã phân công",
        OpTaskStatus.InProgress => "Đang thực hiện",
        OpTaskStatus.Completed => "Hoàn thành",
        OpTaskStatus.Cancelled => "Đã hủy",
        OpTaskStatus.Failed => "Thất bại",
        _ => Status.ToString()
    };
    public string? AssignedToStaffName { get; set; }
    public Guid? AssignedToStaffId { get; set; }
    public string? VehiclePlateNo { get; set; }
    public string? BranchName { get; set; }
}

public class TaskDetailDto : TaskListDto
{
    public Guid? ContractId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleName { get; set; }
    public Guid? BranchId { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaskCreateDto
{
    [Required(ErrorMessage = "Vui lòng chọn loại nhiệm vụ")]
    public TaskType TaskType { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public Guid? ContractId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? AssignedToStaffId { get; set; }
    public Guid? BranchId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian")]
    public DateTime ScheduledAt { get; set; } = DateTime.Now;

    public int EstimatedDurationMinutes { get; set; } = 60;

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class TaskAssignDto
{
    [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
    public Guid StaffId { get; set; }
}

public class TaskUpdateStatusDto
{
    [Required]
    public OpTaskStatus Status { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }
}

public class TaskFilterDto
{
    public string? Search { get; set; }
    public TaskType? TaskType { get; set; }
    public OpTaskStatus? Status { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
