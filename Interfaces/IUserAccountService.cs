using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.UserAccount;

namespace UCar.Interfaces;

public interface IUserAccountService
{
    /// <summary>
    /// Lấy danh sách tài khoản với filter và phân trang
    /// </summary>
    Task<(List<UserAccountListViewModel> Items, int TotalCount, int TotalPages)> GetUserAccountsAsync(
        RoleCode? roleFilter,
        bool? isActiveFilter,
        string? searchTerm,
        int page,
        int pageSize);
    
    /// <summary>
    /// Lấy chi tiết tài khoản
    /// </summary>
    Task<UserAccountDetailsViewModel?> GetUserAccountDetailsAsync(Guid userId);
    
    /// <summary>
    /// Lấy thông tin để edit tài khoản
    /// </summary>
    Task<EditUserAccountViewModel?> GetUserAccountForEditAsync(Guid userId);
    
    /// <summary>
    /// Tạo tài khoản mới
    /// </summary>
    Task<(bool Success, string Message, Guid? UserId)> CreateUserAccountAsync(CreateUserAccountViewModel model);
    
    /// <summary>
    /// Cập nhật tài khoản
    /// </summary>
    Task<(bool Success, string Message)> UpdateUserAccountAsync(EditUserAccountViewModel model);
    
    /// <summary>
    /// Khóa/Mở khóa tài khoản
    /// </summary>
    Task<(bool Success, string Message)> ToggleAccountStatusAsync(Guid userId);
    
    /// <summary>
    /// Reset mật khẩu
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(Guid userId, string newPassword);
    
    /// <summary>
    /// Lấy danh sách vai trò cho dropdown
    /// </summary>
    Task<List<RoleSelectItem>> GetRolesForSelectAsync();
    
    /// <summary>
    /// Kiểm tra username có tồn tại không
    /// </summary>
    Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null);
    
    /// <summary>
    /// Kiểm tra email có tồn tại không
    /// </summary>
    Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null);
}
