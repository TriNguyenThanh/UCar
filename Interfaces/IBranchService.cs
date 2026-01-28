using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;

namespace UCar.Interfaces;

/// <summary>
/// Interface quản lý chi nhánh
/// Tương ứng DFD 8.1 - Quản lý chi nhánh và điểm giao nhận
/// </summary>
public interface IBranchService
{
    /// <summary>Lấy danh sách tất cả chi nhánh</summary>
    Task<IEnumerable<BranchDto>> GetAllAsync();

    /// <summary>Lấy chi tiết chi nhánh theo ID</summary>
    Task<BranchDto?> GetByIdAsync(Guid branchId);

    /// <summary>Tạo chi nhánh mới</summary>
    Task<ServiceResult<Guid>> CreateAsync(BranchCreateDto dto);

    /// <summary>Cập nhật chi nhánh</summary>
    Task<ServiceResult> UpdateAsync(Guid branchId, BranchUpdateDto dto);

    /// <summary>Xóa chi nhánh (không cho xóa nếu có nhân viên/xe)</summary>
    Task<ServiceResult> DeleteAsync(Guid branchId);
}
