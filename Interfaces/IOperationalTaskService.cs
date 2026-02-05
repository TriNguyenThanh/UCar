using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;
using UCar.Models.Enums;

namespace UCar.Interfaces;

/// <summary>
/// Interface quản lý nhiệm vụ vận hành
/// Tương ứng DFD 8.2 - Phân công nhân viên
/// </summary>
public interface IOperationalTaskService
{
    /// <summary>Lấy danh sách nhiệm vụ có phân trang/lọc</summary>
    Task<PagedResult<TaskListDto>> GetTasksAsync(TaskFilterDto filter);

    /// <summary>Lấy chi tiết nhiệm vụ theo ID</summary>
    Task<TaskDetailDto?> GetByIdAsync(Guid taskId);

    /// <summary>Tạo nhiệm vụ mới</summary>
    Task<ServiceResult<Guid>> CreateTaskAsync(TaskCreateDto dto, Guid createdBy);

    /// <summary>Phân công nhiệm vụ cho nhân viên</summary>
    Task<ServiceResult> AssignTaskAsync(Guid taskId, Guid staffId, Guid assignedBy);

    /// <summary>Cập nhật trạng thái nhiệm vụ</summary>
    Task<ServiceResult> UpdateStatusAsync(Guid taskId, TaskUpdateStatusDto dto);

    /// <summary>Lấy danh sách nhiệm vụ của nhân viên theo ngày</summary>
    Task<IEnumerable<TaskListDto>> GetTasksByStaffAsync(Guid staffId, DateTime date);

    /// <summary>Kiểm tra xung đột lịch cho nhân viên</summary>
    Task<bool> HasConflictAsync(Guid staffId, DateTime scheduledAt, int durationMinutes);

    /// <summary>Tự động tạo nhiệm vụ giao xe từ hợp đồng</summary>
    Task<ServiceResult<Guid>> CreateDeliveryTaskFromContractAsync(Guid contractId, Guid createdBy);

    /// <summary>Tự động tạo nhiệm vụ nhận xe từ hợp đồng</summary>
    Task<ServiceResult<Guid>> CreateReturnTaskFromContractAsync(Guid contractId, Guid createdBy);
}
