using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;

namespace UCar.Services;

/// <summary>
/// Service quản lý ca làm việc
/// Tương ứng DFD 8.3 - Quản lý ca làm và hiệu suất
/// </summary>
public class ShiftService : IShiftService
{
    private readonly UCarDbContext _context;
    private readonly IBranchAccessService _branchAccess;

    // Colors for calendar events
    private static readonly string[] ShiftColors =
    {
        "#2196F3", // Blue
        "#4CAF50", // Green
        "#FF9800", // Orange
        "#9C27B0", // Purple
        "#F44336", // Red
        "#00BCD4", // Cyan
    };

    public ShiftService(UCarDbContext context, IBranchAccessService branchAccess)
    {
        _context = context;
        _branchAccess = branchAccess;
    }

    #region Shift CRUD

    public async Task<IEnumerable<ShiftDto>> GetShiftsAsync(Guid? branchId = null)
    {
        var query = _context.Shifts.Include(s => s.Branch).Where(s => s.IsActive).AsQueryable();

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == null || s.BranchId == branchId.Value);
        // When branchId is null, return all shifts (no additional filter)

        return await query
            .OrderBy(s => s.Branch != null ? s.Branch.Name : "")
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftDto
            {
                ShiftId = s.ShiftId,
                ShiftName = s.ShiftName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BranchId = s.BranchId,
                BranchName = s.Branch != null ? s.Branch.Name : "Tất cả chi nhánh",
                Description = s.Description,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<ShiftDto?> GetByIdAsync(Guid shiftId)
    {
        return await _context.Shifts
            .Include(s => s.Branch)
            .Where(s => s.ShiftId == shiftId)
            .Select(s => new ShiftDto
            {
                ShiftId = s.ShiftId,
                ShiftName = s.ShiftName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BranchId = s.BranchId,
                BranchName = s.Branch != null ? s.Branch.Name : "Tất cả chi nhánh",
                Description = s.Description,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateShiftAsync(ShiftCreateDto dto)
    {
        // Check duplicate name in same branch
        var exists = await _context.Shifts.AnyAsync(s =>
            s.ShiftName == dto.ShiftName && s.BranchId == dto.BranchId);

        if (exists)
            return ServiceResult<Guid>.Fail("Tên ca làm đã tồn tại trong chi nhánh này");

        var shift = new Shift
        {
            ShiftId = Guid.NewGuid(),
            ShiftName = dto.ShiftName,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            BranchId = dto.BranchId,
            Description = dto.Description,
            IsActive = true
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(shift.ShiftId, "Tạo ca làm thành công");
    }

    public async Task<ServiceResult> UpdateShiftAsync(Guid shiftId, ShiftUpdateDto dto)
    {
        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null)
            return ServiceResult.Fail("Không tìm thấy ca làm");

        // Check duplicate (excluding current)
        var exists = await _context.Shifts.AnyAsync(s =>
            s.ShiftName == dto.ShiftName &&
            s.BranchId == dto.BranchId &&
            s.ShiftId != shiftId);

        if (exists)
            return ServiceResult.Fail("Tên ca làm đã tồn tại trong chi nhánh này");

        shift.ShiftName = dto.ShiftName;
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;
        shift.BranchId = dto.BranchId;
        shift.Description = dto.Description;
        shift.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật ca làm thành công");
    }

    public async Task<ServiceResult> DeleteShiftAsync(Guid shiftId)
    {
        var shift = await _context.Shifts
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.ShiftId == shiftId);

        if (shift == null)
            return ServiceResult.Fail("Không tìm thấy ca làm");

        if (shift.Assignments.Any())
            return ServiceResult.Fail("Không thể xóa ca làm đã có lịch phân công");

        _context.Shifts.Remove(shift);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa ca làm thành công");
    }

    #endregion

    #region Shift Assignments

    public async Task<IEnumerable<ShiftAssignmentDto>> GetScheduleAsync(ShiftScheduleFilterDto filter)
    {
        var query = _context.ShiftAssignments
            .Include(sa => sa.Shift)
            .Include(sa => sa.Staff)
                .ThenInclude(s => s.Branch)
            .AsQueryable();

        if (filter.BranchId.HasValue)
            query = query.Where(sa => sa.Staff.BranchId == filter.BranchId.Value);

        if (filter.StaffId.HasValue)
            query = query.Where(sa => sa.StaffId == filter.StaffId.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(sa => sa.WorkDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(sa => sa.WorkDate <= filter.DateTo.Value);

        return await query
            .OrderBy(sa => sa.WorkDate)
            .ThenBy(sa => sa.Shift.StartTime)
            .Select(sa => new ShiftAssignmentDto
            {
                AssignmentId = sa.AssignmentId,
                ShiftId = sa.ShiftId,
                ShiftName = sa.Shift.ShiftName,
                StartTime = sa.Shift.StartTime,
                EndTime = sa.Shift.EndTime,
                StaffId = sa.StaffId,
                StaffName = sa.Staff.FullName,
                StaffCode = sa.Staff.StaffCode,
                BranchName = sa.Staff.Branch != null ? sa.Staff.Branch.Name : "",
                WorkDate = sa.WorkDate,
                Notes = sa.Notes
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(ShiftScheduleFilterDto filter)
    {
        var assignments = await GetScheduleAsync(filter);
        var events = new List<CalendarEventDto>();
        var showBranchName = !filter.BranchId.HasValue; // Show branch name when viewing all branches

        // Get distinct shifts for color mapping
        var shiftIds = assignments.Select(a => a.ShiftId).Distinct().ToList();
        var colorMap = shiftIds.Select((id, index) => new { id, color = ShiftColors[index % ShiftColors.Length] })
                               .ToDictionary(x => x.id, x => x.color);

        foreach (var a in assignments)
        {
            var startDateTime = a.WorkDate.ToDateTime(a.StartTime);
            var endDateTime = a.WorkDate.ToDateTime(a.EndTime);

            // Handle overnight shifts
            if (a.EndTime < a.StartTime)
                endDateTime = endDateTime.AddDays(1);

            // Format title with branch name if viewing all branches
            var title = showBranchName && !string.IsNullOrEmpty(a.BranchName)
                ? $"{a.StaffName} ({a.BranchName}) - {a.ShiftName}"
                : $"{a.StaffName} - {a.ShiftName}";

            events.Add(new CalendarEventDto
            {
                Id = a.AssignmentId.ToString(),
                Title = title,
                Start = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                End = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                BackgroundColor = colorMap.GetValueOrDefault(a.ShiftId, "#2196F3"),
                BorderColor = colorMap.GetValueOrDefault(a.ShiftId, "#1976D2"),
                AllDay = false,
                ExtendedProps = new
                {
                    staffId = a.StaffId,
                    staffCode = a.StaffCode,
                    shiftId = a.ShiftId,
                    branchName = a.BranchName,
                    notes = a.Notes
                }
            });
        }

        return events;
    }

    public async Task<ServiceResult<Guid>> AssignShiftAsync(ShiftAssignmentCreateDto dto, Guid createdBy)
    {
        // Check shift exists
        var shift = await _context.Shifts.FindAsync(dto.ShiftId);
        if (shift == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy ca làm");

        // Check staff exists
        var staff = await _context.StaffProfiles.FindAsync(dto.StaffId);
        if (staff == null || !staff.IsActive)
            return ServiceResult<Guid>.Fail("Không tìm thấy nhân viên hoặc nhân viên đã ngừng hoạt động");

        // Check matching branch
        if (shift.BranchId.HasValue && shift.BranchId != staff.BranchId)
            return ServiceResult<Guid>.Fail($"Không thể phân công nhân viên chi nhánh khác vào ca làm của chi nhánh này");

        // Check conflict
        if (await HasConflictAsync(dto.StaffId, dto.ShiftId, dto.WorkDate))
            return ServiceResult<Guid>.Fail("Nhân viên đã được phân công ca khác trong ngày này");

        var assignment = new ShiftAssignment
        {
            AssignmentId = Guid.NewGuid(),
            ShiftId = dto.ShiftId,
            StaffId = dto.StaffId,
            WorkDate = dto.WorkDate,
            Notes = dto.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(assignment.AssignmentId, "Phân công ca làm thành công");
    }

    public async Task<ServiceResult> RemoveAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _context.ShiftAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return ServiceResult.Fail("Không tìm thấy phân công");

        // Don't allow removing past assignments
        if (assignment.WorkDate < DateOnly.FromDateTime(DateTime.Today))
            return ServiceResult.Fail("Không thể xóa phân công ca đã qua");

        _context.ShiftAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa phân công thành công");
    }

    public async Task<bool> HasConflictAsync(Guid staffId, Guid shiftId, DateOnly workDate)
    {
        // Simple check: one shift per day per staff (can be made more complex)
        return await _context.ShiftAssignments
            .AnyAsync(sa => sa.StaffId == staffId && sa.WorkDate == workDate);
    }

    public async Task<ServiceResult<int>> BulkAssignAsync(Guid shiftId, Guid staffId, DateOnly fromDate, DateOnly toDate, Guid createdBy)
    {
        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null)
            return ServiceResult<int>.Fail("Không tìm thấy ca làm");

        var staff = await _context.StaffProfiles.FindAsync(staffId);
        if (staff == null || !staff.IsActive)
            return ServiceResult<int>.Fail("Không tìm thấy nhân viên");

        var existingDates = await _context.ShiftAssignments
            .Where(sa => sa.StaffId == staffId && sa.WorkDate >= fromDate && sa.WorkDate <= toDate)
            .Select(sa => sa.WorkDate)
            .ToListAsync();

        var count = 0;
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (existingDates.Contains(date))
                continue;

            _context.ShiftAssignments.Add(new ShiftAssignment
            {
                AssignmentId = Guid.NewGuid(),
                ShiftId = shiftId,
                StaffId = staffId,
                WorkDate = date,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            });
            count++;
        }

        await _context.SaveChangesAsync();

        return ServiceResult<int>.Ok(count, $"Đã phân công {count} ngày làm việc");
    }

    #endregion
}
