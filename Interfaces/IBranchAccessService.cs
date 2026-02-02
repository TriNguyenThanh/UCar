using UCar.Models.Enums;

namespace UCar.Interfaces;

/// <summary>
/// Service xác định quyền truy cập theo chi nhánh
/// Admin: có quyền truy cập tất cả chi nhánh (BranchId = null)
/// BranchManager: chỉ truy cập chi nhánh mình quản lý
/// Staff: chỉ truy cập chi nhánh mình thuộc về
/// </summary>
public interface IBranchAccessService
{
    /// <summary>
    /// Lấy BranchId của user hiện tại.
    /// Trả về null nếu là Admin (có quyền xem tất cả chi nhánh)
    /// </summary>
    Guid? GetCurrentUserBranchId();
    
    /// <summary>
    /// Kiểm tra user hiện tại có phải Admin không
    /// </summary>
    bool IsAdmin();
    
    /// <summary>
    /// Kiểm tra user hiện tại có phải BranchManager không
    /// </summary>
    bool IsBranchManager();
    
    /// <summary>
    /// Kiểm tra user hiện tại có quyền truy cập chi nhánh cụ thể không
    /// Admin: luôn có quyền
    /// BranchManager/Staff: chỉ có quyền nếu BranchId trùng với chi nhánh của họ
    /// </summary>
    bool CanAccessBranch(Guid branchId);
    
    /// <summary>
    /// Lấy RoleCode của user hiện tại
    /// </summary>
    RoleCode? GetCurrentUserRole();
    
    /// <summary>
    /// Lấy UserId của user hiện tại
    /// </summary>
    Guid? GetCurrentUserId();
}
