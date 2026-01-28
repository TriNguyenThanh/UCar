using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Services;

public class AuthService : IAuthService
{
    private readonly UCarDbContext _context;

    public AuthService(UCarDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccount?> AuthenticateAsync(string username, string password)
    {
        var user = await _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Customer)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
            return null;

        // In production, use proper password hashing (BCrypt, etc.)
        // For now, comparing directly (assuming seed data uses plain text or you implement proper hashing)
        if (VerifyPassword(password, user.PasswordHash))
        {
            return user;
        }

        return null;
    }

    public async Task<UserAccount?> GetUserByIdAsync(Guid userId)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Customer)
            .Include(u => u.StaffProfile!)
                .ThenInclude(s => s.Branch)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _context.UserAccounts.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> GetName(UserAccount user)
    {
        var role = GetRole(user);
        return role switch
        {
            RoleCode.Customer => user.Customer?.FullName ?? string.Empty,
            RoleCode.Staff => user.StaffProfile?.FullName ?? string.Empty,
            _ => string.Empty
        };
    }
    private bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    private RoleCode GetRole(UserAccount user)
    {
        var roleName = _context.Roles
            .Where(r => r.RoleId == user.RoleId)
            .Select(r => r.Code)
            .FirstOrDefault();

        return roleName;
    }
}
