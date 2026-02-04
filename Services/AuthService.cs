using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels;
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
    public async Task<(bool Success, string ErrorMessage, UserAccount? User)> RegisterCustomerAsync(RegisterViewModel model)
    {
        // 1. Check if username exists
        if (await _context.UserAccounts.AnyAsync(u => u.Username == model.Username))
        {
            return (false, "Tên đăng nhập đã tồn tại", null);
        }

        // 2. Check if email exists
        if (await _context.UserAccounts.AnyAsync(u => u.Email == model.Email))
        {
            return (false, "Email đã được sử dụng", null);
        }

        // 3. Get Role Customer
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Customer);
        if (customerRole == null)
        {
            return (false, "Lỗi hệ thống: Không tìm thấy vai trò Customer", null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 4. Create UserAccount
            var userId = Guid.NewGuid();
            var user = new UserAccount
            {
                UserId = userId,
                RoleId = customerRole.RoleId,
                Username = model.Username,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = HashPassword(model.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            // 5. Create Customer profile
            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                UserId = userId,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                RiskLevel = "Low"
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return (true, string.Empty, user);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return (false, "Đã xảy ra lỗi khi tạo tài khoản", null);
        }
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy người dùng");
            }

            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                return (false, "Mật khẩu hiện tại không đúng");
            }

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return (true, "Đổi mật khẩu thành công");
        }
        catch (Exception ex)
        {
            return (false, "Đã xảy ra lỗi khi đổi mật khẩu");
        }
    }
}
