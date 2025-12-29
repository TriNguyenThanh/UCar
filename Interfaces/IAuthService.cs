using UCar.Models;

namespace UCar.Interfaces;

public interface IAuthService
{
    Task<UserAccount?> AuthenticateAsync(string username, string password);
    Task<UserAccount?> GetUserByIdAsync(Guid userId);
    Task UpdateLastLoginAsync(Guid userId);
}
