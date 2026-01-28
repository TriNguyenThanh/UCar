using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;

namespace UCar.Interfaces;

/// <summary>
/// Interface quản lý nhân viên
/// Tương ứng DFD 8.3 - Quản lý nhân sự
/// </summary>
public interface IStaffService
{
    /// <summary>Lấy danh sách nhân viên có phân trang/lọc</summary>
    Task<PagedResult<StaffListDto>> GetStaffListAsync(StaffFilterDto filter);

    /// <summary>Lấy chi tiết nhân viên theo ID</summary>
    Task<StaffDetailDto?> GetByIdAsync(Guid staffId);

    /// <summary>Tạo nhân viên mới kèm tài khoản đăng nhập</summary>
    Task<ServiceResult<Guid>> CreateStaffAsync(StaffCreateDto dto, Guid createdBy);

    /// <summary>Cập nhật thông tin nhân viên</summary>
    Task<ServiceResult> UpdateStaffAsync(Guid staffId, StaffUpdateDto dto);

    /// <summary>Vô hiệu hóa nhân viên (soft delete)</summary>
    Task<ServiceResult> DeactivateStaffAsync(Guid staffId);

    /// <summary>Kích hoạt lại nhân viên</summary>
    Task<ServiceResult> ActivateStaffAsync(Guid staffId);

    /// <summary>Reset mật khẩu nhân viên</summary>
    Task<ServiceResult> ResetPasswordAsync(Guid staffId, string newPassword);

    /// <summary>Lấy hiệu suất nhân viên trong khoảng thời gian</summary>
    Task<StaffPerformanceDto> GetPerformanceAsync(Guid staffId, DateTime from, DateTime to);

    /// <summary>Lấy danh sách nhân viên có sẵn để phân công</summary>
    Task<IEnumerable<StaffListDto>> GetAvailableStaffAsync(Guid? branchId = null);
}
