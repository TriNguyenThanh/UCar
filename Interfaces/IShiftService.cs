using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;

namespace UCar.Interfaces;

/// <summary>
/// Interface quản lý ca làm việc
/// Tương ứng DFD 8.3 - Quản lý ca làm và hiệu suất
/// </summary>
public interface IShiftService
{
    #region Shift CRUD

    /// <summary>Lấy danh sách ca làm</summary>
    Task<IEnumerable<ShiftDto>> GetShiftsAsync(Guid? branchId = null);

    /// <summary>Lấy chi tiết ca làm theo ID</summary>
    Task<ShiftDto?> GetByIdAsync(Guid shiftId);

    /// <summary>Tạo ca làm mới</summary>
    Task<ServiceResult<Guid>> CreateShiftAsync(ShiftCreateDto dto);

    /// <summary>Cập nhật ca làm</summary>
    Task<ServiceResult> UpdateShiftAsync(Guid shiftId, ShiftUpdateDto dto);

    /// <summary>Xóa ca làm (không cho xóa nếu đã có lịch phân công)</summary>
    Task<ServiceResult> DeleteShiftAsync(Guid shiftId);

    #endregion

    #region Shift Assignments

    /// <summary>Lấy lịch phân công ca theo khoảng thời gian</summary>
    Task<IEnumerable<ShiftAssignmentDto>> GetScheduleAsync(ShiftScheduleFilterDto filter);

    /// <summary>Lấy lịch dạng calendar events (cho FullCalendar)</summary>
    Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(ShiftScheduleFilterDto filter);

    /// <summary>Phân công ca làm cho nhân viên</summary>
    Task<ServiceResult<Guid>> AssignShiftAsync(ShiftAssignmentCreateDto dto, Guid createdBy);

    /// <summary>Xóa phân công ca</summary>
    Task<ServiceResult> RemoveAssignmentAsync(Guid assignmentId);

    /// <summary>Kiểm tra xung đột ca làm</summary>
    Task<bool> HasConflictAsync(Guid staffId, Guid shiftId, DateOnly workDate);

    /// <summary>Phân công hàng loạt (nhiều ngày)</summary>
    Task<ServiceResult<int>> BulkAssignAsync(Guid shiftId, Guid staffId, DateOnly fromDate, DateOnly toDate, Guid createdBy);

    #endregion
}
