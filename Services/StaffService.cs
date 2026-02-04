using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý nhân viên
/// Tương ứng DFD 8.3 - Quản lý nhân sự
/// </summary>
public class StaffService : IStaffService
{
    private readonly UCarDbContext _context;
    private readonly IBranchAccessService _branchAccess;

    public StaffService(UCarDbContext context, IBranchAccessService branchAccess)
    {
        _context = context;
        _branchAccess = branchAccess;
    }

    public async Task<PagedResult<StaffListDto>> GetStaffListAsync(StaffFilterDto filter)
    {
        var query = _context.StaffProfiles
            .Include(s => s.UserAccount)
            .Include(s => s.Branch)
            .AsQueryable();

        // *** BRANCH ACCESS FILTER ***
        // BranchManager chỉ thấy nhân viên thuộc chi nhánh mình
        var userBranchId = _branchAccess.GetCurrentUserBranchId();
        if (userBranchId.HasValue)
        {
            query = query.Where(s => s.BranchId == userBranchId.Value);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(search) ||
                s.StaffCode.ToLower().Contains(search) ||
                (s.UserAccount.Email != null && s.UserAccount.Email.ToLower().Contains(search)));
        }

        // Chỉ filter thêm theo BranchId nếu Admin muốn lọc theo chi nhánh cụ thể
        if (filter.BranchId.HasValue && !userBranchId.HasValue)
            query = query.Where(s => s.BranchId == filter.BranchId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Position))
            query = query.Where(s => s.Position == filter.Position);

        if (filter.IsActive.HasValue)
            query = query.Where(s => s.IsActive == filter.IsActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.FullName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new StaffListDto
            {
                StaffId = s.StaffId,
                StaffCode = s.StaffCode,
                FullName = s.FullName,
                Position = s.Position,
                Email = s.UserAccount.Email,
                Phone = s.UserAccount.Phone,
                BranchId = s.BranchId,
                BranchName = s.Branch.Name,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return new PagedResult<StaffListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<StaffDetailDto?> GetByIdAsync(Guid staffId)
    {
        return await _context.StaffProfiles
            .Include(s => s.UserAccount)
            .Include(s => s.Branch)
            .Where(s => s.StaffId == staffId)
            .Select(s => new StaffDetailDto
            {
                StaffId = s.StaffId,
                StaffCode = s.StaffCode,
                FullName = s.FullName,
                Position = s.Position,
                Email = s.UserAccount.Email,
                Phone = s.UserAccount.Phone,
                BranchId = s.BranchId,
                BranchName = s.Branch.Name,
                IsActive = s.IsActive,
                UserId = s.UserId,
                Username = s.UserAccount.Username,
                LastLoginAt = s.UserAccount.LastLoginAt,
                CreatedAt = s.UserAccount.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateStaffAsync(StaffCreateDto dto, Guid createdBy)
    {
        // Check duplicate staff code
        if (await _context.StaffProfiles.AnyAsync(s => s.StaffCode == dto.StaffCode))
            return ServiceResult<Guid>.Fail("Mã nhân viên đã tồn tại");

        // Check duplicate username
        if (await _context.UserAccounts.AnyAsync(u => u.Username == dto.Username))
            return ServiceResult<Guid>.Fail("Tên đăng nhập đã tồn tại");

        // Check email
        if (!string.IsNullOrEmpty(dto.Email) && await _context.UserAccounts.AnyAsync(u => u.Email == dto.Email))
            return ServiceResult<Guid>.Fail("Email đã được sử dụng");

        // Get Roles
        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Staff);
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Admin);

        if (staffRole == null || adminRole == null)
            return ServiceResult<Guid>.Fail("Lỗi hệ thống: Không tìm thấy vai trò (Roles)");

        // Determine Role based on Position
        var targetRole = dto.Position == "Quản lý" ? adminRole : staffRole;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create UserAccount
            var userAccount = new UserAccount
            {
                UserId = Guid.NewGuid(),
                RoleId = targetRole.RoleId,
                Username = dto.Username,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(userAccount);

            // Create StaffProfile
            var staff = new StaffProfile
            {
                StaffId = Guid.NewGuid(),
                UserId = userAccount.UserId,
                BranchId = dto.BranchId,
                StaffCode = dto.StaffCode,
                FullName = dto.FullName,
                Position = dto.Position,
                IsActive = true
            };

            _context.StaffProfiles.Add(staff);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<Guid>.Ok(staff.StaffId, "Tạo nhân viên thành công");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<Guid>.Fail($"Lỗi khi tạo nhân viên: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateStaffAsync(Guid staffId, StaffUpdateDto dto)
    {
        var staff = await _context.StaffProfiles
            .Include(s => s.UserAccount)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);

        if (staff == null)
            return ServiceResult.Fail("Không tìm thấy nhân viên");

        // Check email duplicate (excluding current)
        if (!string.IsNullOrEmpty(dto.Email) &&
            await _context.UserAccounts.AnyAsync(u => u.Email == dto.Email && u.UserId != staff.UserId))
            return ServiceResult.Fail("Email đã được sử dụng");

        staff.FullName = dto.FullName;
        staff.Position = dto.Position;
        staff.BranchId = dto.BranchId;
        staff.IsActive = dto.IsActive;

        // Update Role if Position changed
        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Staff);
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Admin);

        if (staffRole != null && adminRole != null)
        {
            var targetRole = dto.Position == "Quản lý" ? adminRole : staffRole;
            if (staff.UserAccount.RoleId != targetRole.RoleId)
            {
                staff.UserAccount.RoleId = targetRole.RoleId;
            }
        }

        staff.UserAccount.Email = dto.Email;
        staff.UserAccount.Phone = dto.Phone;
        staff.UserAccount.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật nhân viên thành công");
    }

    public async Task<ServiceResult> DeactivateStaffAsync(Guid staffId)
    {
        var staff = await _context.StaffProfiles
            .Include(s => s.UserAccount)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);

        if (staff == null)
            return ServiceResult.Fail("Không tìm thấy nhân viên");

        // Check if has pending tasks
        var hasPendingTasks = await _context.OperationalTasks
            .AnyAsync(t => t.AssignedToStaffId == staffId &&
                (t.Status == OpTaskStatus.Assigned || t.Status == OpTaskStatus.InProgress));

        if (hasPendingTasks)
            return ServiceResult.Fail("Không thể vô hiệu hóa nhân viên đang có nhiệm vụ chưa hoàn thành");

        staff.IsActive = false;
        staff.UserAccount.IsActive = false;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Đã vô hiệu hóa nhân viên");
    }

    public async Task<ServiceResult> ActivateStaffAsync(Guid staffId)
    {
        var staff = await _context.StaffProfiles
            .Include(s => s.UserAccount)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);

        if (staff == null)
            return ServiceResult.Fail("Không tìm thấy nhân viên");

        staff.IsActive = true;
        staff.UserAccount.IsActive = true;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Đã kích hoạt lại nhân viên");
    }

    public async Task<ServiceResult> ResetPasswordAsync(Guid staffId, string newPassword)
    {
        var staff = await _context.StaffProfiles
            .Include(s => s.UserAccount)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);

        if (staff == null)
            return ServiceResult.Fail("Không tìm thấy nhân viên");

        staff.UserAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Đã reset mật khẩu thành công");
    }

    public async Task<StaffPerformanceDto> GetPerformanceAsync(Guid staffId, DateTime from, DateTime to)
    {
        var staff = await _context.StaffProfiles.FindAsync(staffId);
        if (staff == null)
            return new StaffPerformanceDto { StaffId = staffId };

        var tasks = await _context.OperationalTasks
            .Where(t => t.AssignedToStaffId == staffId &&
                        t.ScheduledAt >= from && t.ScheduledAt <= to)
            .ToListAsync();

        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == OpTaskStatus.Completed);
        var failedTasks = tasks.Count(t => t.Status == OpTaskStatus.Failed);
        var cancelledTasks = tasks.Count(t => t.Status == OpTaskStatus.Cancelled);

        var onTimeTasks = tasks.Count(t =>
            t.Status == OpTaskStatus.Completed &&
            t.CompletedAt.HasValue &&
            t.CompletedAt.Value <= t.ScheduledAt.AddMinutes(t.EstimatedDurationMinutes));

        var shiftsWorked = await _context.ShiftAssignments
            .CountAsync(sa => sa.StaffId == staffId &&
                              sa.WorkDate >= DateOnly.FromDateTime(from) &&
                              sa.WorkDate <= DateOnly.FromDateTime(to));

        return new StaffPerformanceDto
        {
            StaffId = staffId,
            StaffName = staff.FullName,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            FailedTasks = failedTasks,
            CancelledTasks = cancelledTasks,
            CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0,
            OnTimeRate = completedTasks > 0 ? (double)onTimeTasks / completedTasks * 100 : 0,
            TotalShiftsWorked = shiftsWorked
        };
    }

    public async Task<IEnumerable<StaffListDto>> GetAvailableStaffAsync(Guid? branchId = null)
    {
        var query = _context.StaffProfiles
            .Include(s => s.UserAccount)
            .Include(s => s.Branch)
            .Where(s => s.IsActive);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        return await query
            .OrderBy(s => s.FullName)
            .Select(s => new StaffListDto
            {
                StaffId = s.StaffId,
                StaffCode = s.StaffCode,
                FullName = s.FullName,
                Position = s.Position,
                BranchId = s.BranchId,
                BranchName = s.Branch.Name,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }
}
