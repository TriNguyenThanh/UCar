using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.UserAccount;

namespace UCar.Services;

public class UserAccountService : IUserAccountService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(UCarDbContext context, ILogger<UserAccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<UserAccountListViewModel> Items, int TotalCount, int TotalPages)> GetUserAccountsAsync(
        RoleCode? roleFilter,
        bool? isActiveFilter,
        string? searchTerm,
        int page,
        int pageSize)
    {
        var query = _context.UserAccounts
            .Include(u => u.Role)
            .Include(u => u.Customer)
            .Include(u => u.StaffProfile)
            .AsQueryable();

        // Apply filters
        if (roleFilter.HasValue)
        {
            query = query.Where(u => u.Role.Code == roleFilter.Value);
        }

        if (isActiveFilter.HasValue)
        {
            query = query.Where(u => u.IsActive == isActiveFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Phone != null && u.Phone.Contains(term)) ||
                (u.Customer != null && u.Customer.FullName.ToLower().Contains(term)) ||
                (u.StaffProfile != null && u.StaffProfile.FullName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserAccountListViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                RoleName = u.Role.Name,
                RoleCode = u.Role.Code,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                LinkedProfileName = u.Customer != null ? u.Customer.FullName :
                                   u.StaffProfile != null ? u.StaffProfile.FullName : null,
                LinkedProfileType = u.Customer != null ? "Khách hàng" :
                                   u.StaffProfile != null ? "Nhân viên" : null
            })
            .ToListAsync();

        return (items, totalCount, totalPages);
    }

    public async Task<UserAccountDetailsViewModel?> GetUserAccountDetailsAsync(Guid userId)
    {
        var user = await _context.UserAccounts
            .Include(u => u.Role)
            .Include(u => u.Customer)
            .Include(u => u.StaffProfile)
                .ThenInclude(s => s!.Branch)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return null;

        return new UserAccountDetailsViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            RoleCode = user.Role.Code,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            CustomerId = user.Customer?.CustomerId,
            CustomerName = user.Customer?.FullName,
            StaffId = user.StaffProfile?.StaffId,
            StaffName = user.StaffProfile?.FullName,
            StaffPosition = user.StaffProfile?.Position,
            BranchName = user.StaffProfile?.Branch?.Name
        };
    }

    public async Task<EditUserAccountViewModel?> GetUserAccountForEditAsync(Guid userId)
    {
        var user = await _context.UserAccounts
            .Include(u => u.Role)
            .Include(u => u.Customer)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return null;

        return new EditUserAccountViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            LinkedProfileName = user.Customer?.FullName ?? user.StaffProfile?.FullName,
            LinkedProfileType = user.Customer != null ? "Khách hàng" :
                               user.StaffProfile != null ? "Nhân viên" : null,
            AvailableRoles = await GetRolesForSelectAsync()
        };
    }

    public async Task<(bool Success, string Message, Guid? UserId)> CreateUserAccountAsync(CreateUserAccountViewModel model)
    {
        try
        {
            // Check username exists
            if (await IsUsernameExistsAsync(model.Username))
            {
                return (false, "Tên đăng nhập đã tồn tại", null);
            }

            // Check email exists
            if (!string.IsNullOrWhiteSpace(model.Email) && await IsEmailExistsAsync(model.Email))
            {
                return (false, "Email đã được sử dụng", null);
            }

            // Verify role exists
            var role = await _context.Roles.FindAsync(model.RoleId);
            if (role == null)
            {
                return (false, "Vai trò không tồn tại", null);
            }

            var user = new UserAccount
            {
                UserId = Guid.NewGuid(),
                Username = model.Username,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user account: {Username} with role {Role}", user.Username, role.Name);

            return (true, "Tạo tài khoản thành công", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user account");
            return (false, "Có lỗi xảy ra khi tạo tài khoản", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdateUserAccountAsync(EditUserAccountViewModel model)
    {
        try
        {
            var user = await _context.UserAccounts.FindAsync(model.UserId);
            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản");
            }

            // Check username exists (exclude current user)
            if (await IsUsernameExistsAsync(model.Username, model.UserId))
            {
                return (false, "Tên đăng nhập đã tồn tại");
            }

            // Check email exists (exclude current user)
            if (!string.IsNullOrWhiteSpace(model.Email) && await IsEmailExistsAsync(model.Email, model.UserId))
            {
                return (false, "Email đã được sử dụng");
            }

            // Verify role exists
            var role = await _context.Roles.FindAsync(model.RoleId);
            if (role == null)
            {
                return (false, "Vai trò không tồn tại");
            }

            user.Username = model.Username;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user account: {Username}", user.Username);

            return (true, "Cập nhật tài khoản thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user account {UserId}", model.UserId);
            return (false, "Có lỗi xảy ra khi cập nhật tài khoản");
        }
    }

    public async Task<(bool Success, string Message)> ToggleAccountStatusAsync(Guid userId)
    {
        try
        {
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản");
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "kích hoạt" : "khóa";
            _logger.LogInformation("Toggled user account status: {Username} is now {Status}", user.Username, status);

            return (true, $"Đã {status} tài khoản thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling account status for {UserId}", userId);
            return (false, "Có lỗi xảy ra");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset password for user: {Username}", user.Username);

            return (true, "Đặt lại mật khẩu thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {UserId}", userId);
            return (false, "Có lỗi xảy ra khi đặt lại mật khẩu");
        }
    }

    public async Task<List<RoleSelectItem>> GetRolesForSelectAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Code)
            .Select(r => new RoleSelectItem
            {
                RoleId = r.RoleId,
                Name = r.Name,
                Code = r.Code
            })
            .ToListAsync();
    }

    public async Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null)
    {
        var query = _context.UserAccounts.Where(u => u.Username.ToLower() == username.ToLower());
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null)
    {
        var query = _context.UserAccounts.Where(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }
}
