using UCar.Models;
using UCar.ViewModels;
namespace UCar.Interfaces;

public interface IAuthService
{
    // Bổ sung hợp lý: Đổi mật khẩu
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<UserAccount?> AuthenticateAsync(string username, string password);
    Task<UserAccount?> GetUserByIdAsync(Guid userId);
    Task<string> GetName(UserAccount user);
    Task UpdateLastLoginAsync(Guid userId);
    Task<(bool Success, string ErrorMessage, UserAccount? User)> RegisterCustomerAsync(RegisterViewModel model);
}
