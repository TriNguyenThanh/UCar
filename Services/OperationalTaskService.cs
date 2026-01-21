using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý nhiệm vụ vận hành
/// Tương ứng DFD 8.2 - Phân công nhân viên
/// </summary>
public class OperationalTaskService : IOperationalTaskService
{
    private readonly UCarDbContext _context;

    public OperationalTaskService(UCarDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TaskListDto>> GetTasksAsync(TaskFilterDto filter)
    {
        var query = _context.OperationalTasks
            .Include(t => t.AssignedToStaff)
            .Include(t => t.Vehicle)
            .Include(t => t.Branch)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(search));
        }

        if (filter.TaskType.HasValue)
            query = query.Where(t => t.TaskType == filter.TaskType.Value);

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(t => t.BranchId == filter.BranchId.Value);

        if (filter.StaffId.HasValue)
            query = query.Where(t => t.AssignedToStaffId == filter.StaffId.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.ScheduledAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(t => t.ScheduledAt <= filter.DateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.ScheduledAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => MapToListDto(t))
            .ToListAsync();

        return new PagedResult<TaskListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TaskDetailDto?> GetByIdAsync(Guid taskId)
    {
        var task = await _context.OperationalTasks
            .Include(t => t.AssignedToStaff)
            .Include(t => t.Vehicle)
                .ThenInclude(v => v!.Model)
            .Include(t => t.Branch)
            .Include(t => t.RentalContract)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);

        if (task == null) return null;

        return new TaskDetailDto
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            Title = task.Title,
            ScheduledAt = task.ScheduledAt,
            Location = task.Location,
            Status = task.Status,
            AssignedToStaffId = task.AssignedToStaffId,
            AssignedToStaffName = task.AssignedToStaff?.FullName,
            VehiclePlateNo = task.Vehicle?.PlateNo,
            VehicleId = task.VehicleId,
            VehicleName = task.Vehicle?.Model != null ? $"{task.Vehicle.Model.Make} {task.Vehicle.Model.ModelName}" : null,
            BranchId = task.BranchId,
            BranchName = task.Branch?.Name,
            ContractId = task.ContractId,
            EstimatedDurationMinutes = task.EstimatedDurationMinutes,
            CompletedAt = task.CompletedAt,
            Notes = task.Notes,
            FailureReason = task.FailureReason,
            CreatedAt = task.CreatedAt
        };
    }

    public async Task<ServiceResult<Guid>> CreateTaskAsync(TaskCreateDto dto, Guid createdBy)
    {
        var task = new OperationalTask
        {
            TaskId = Guid.NewGuid(),
            TaskType = dto.TaskType,
            Title = dto.Title,
            ContractId = dto.ContractId,
            VehicleId = dto.VehicleId,
            AssignedToStaffId = dto.AssignedToStaffId,
            BranchId = dto.BranchId,
            ScheduledAt = dto.ScheduledAt,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            Location = dto.Location,
            Notes = dto.Notes,
            Status = dto.AssignedToStaffId.HasValue ? OpTaskStatus.Assigned : OpTaskStatus.New,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.OperationalTasks.Add(task);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(task.TaskId, "Tạo nhiệm vụ thành công");
    }

    public async Task<ServiceResult> AssignTaskAsync(Guid taskId, Guid staffId, Guid assignedBy)
    {
        var task = await _context.OperationalTasks.FindAsync(taskId);
        if (task == null)
            return ServiceResult.Fail("Không tìm thấy nhiệm vụ");

        if (task.Status != OpTaskStatus.New && task.Status != OpTaskStatus.Assigned)
            return ServiceResult.Fail("Không thể phân công nhiệm vụ ở trạng thái hiện tại");

        // Check staff exists and is active
        var staff = await _context.StaffProfiles.FindAsync(staffId);
        if (staff == null || !staff.IsActive)
            return ServiceResult.Fail("Không tìm thấy nhân viên hoặc nhân viên đã ngừng hoạt động");

        // Check conflict
        if (await HasConflictAsync(staffId, task.ScheduledAt, task.EstimatedDurationMinutes))
            return ServiceResult.Fail("Nhân viên đã có nhiệm vụ khác trong khung giờ này");

        task.AssignedToStaffId = staffId;
        task.Status = OpTaskStatus.Assigned;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Phân công nhiệm vụ thành công");
    }

    public async Task<ServiceResult> UpdateStatusAsync(Guid taskId, TaskUpdateStatusDto dto)
    {
        var task = await _context.OperationalTasks.FindAsync(taskId);
        if (task == null)
            return ServiceResult.Fail("Không tìm thấy nhiệm vụ");

        // Validate state transition
        var validTransition = IsValidStatusTransition(task.Status, dto.Status);
        if (!validTransition)
            return ServiceResult.Fail($"Không thể chuyển từ trạng thái '{task.Status}' sang '{dto.Status}'");

        task.Status = dto.Status;

        if (!string.IsNullOrEmpty(dto.Notes))
            task.Notes = (task.Notes ?? "") + "\n" + dto.Notes;

        if (dto.Status == OpTaskStatus.Completed)
            task.CompletedAt = DateTime.UtcNow;

        if (dto.Status == OpTaskStatus.Failed || dto.Status == OpTaskStatus.Cancelled)
            task.FailureReason = dto.FailureReason;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật trạng thái thành công");
    }

    public async Task<IEnumerable<TaskListDto>> GetTasksByStaffAsync(Guid staffId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var tasks = await _context.OperationalTasks
            .Include(t => t.Vehicle)
            .Include(t => t.Branch)
            .Where(t => t.AssignedToStaffId == staffId &&
                        t.ScheduledAt >= startOfDay && t.ScheduledAt < endOfDay)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();

        return tasks.Select(t => MapToListDto(t));
    }

    public async Task<bool> HasConflictAsync(Guid staffId, DateTime scheduledAt, int durationMinutes)
    {
        var endTime = scheduledAt.AddMinutes(durationMinutes);

        return await _context.OperationalTasks
            .AnyAsync(t => t.AssignedToStaffId == staffId &&
                          t.Status != OpTaskStatus.Cancelled &&
                          t.Status != OpTaskStatus.Failed &&
                          t.Status != OpTaskStatus.Completed &&
                          ((t.ScheduledAt < endTime && t.ScheduledAt.AddMinutes(t.EstimatedDurationMinutes) > scheduledAt)));
    }

    public async Task<ServiceResult<Guid>> CreateDeliveryTaskFromContractAsync(Guid contractId, Guid createdBy)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hợp đồng");

        var dto = new TaskCreateDto
        {
            TaskType = TaskType.Delivery,
            Title = $"Giao xe {contract.Vehicle?.PlateNo} - HĐ {contract.ContractId.ToString()[..8]}",
            ContractId = contractId,
            VehicleId = contract.VehicleId,
            BranchId = contract.Vehicle?.BranchId,
            ScheduledAt = contract.Booking?.StartAt ?? contract.PlannedStart,
            EstimatedDurationMinutes = 30
        };

        return await CreateTaskAsync(dto, createdBy);
    }

    public async Task<ServiceResult<Guid>> CreateReturnTaskFromContractAsync(Guid contractId, Guid createdBy)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hợp đồng");

        var dto = new TaskCreateDto
        {
            TaskType = TaskType.Return,
            Title = $"Nhận xe {contract.Vehicle?.PlateNo} - HĐ {contract.ContractId.ToString()[..8]}",
            ContractId = contractId,
            VehicleId = contract.VehicleId,
            BranchId = contract.Vehicle?.BranchId,
            ScheduledAt = contract.Booking?.EndAt ?? contract.PlannedEnd,
            EstimatedDurationMinutes = 30
        };

        return await CreateTaskAsync(dto, createdBy);
    }

    private static TaskListDto MapToListDto(OperationalTask t)
    {
        return new TaskListDto
        {
            TaskId = t.TaskId,
            TaskType = t.TaskType,
            Title = t.Title,
            ScheduledAt = t.ScheduledAt,
            Location = t.Location,
            Status = t.Status,
            AssignedToStaffId = t.AssignedToStaffId,
            AssignedToStaffName = t.AssignedToStaff?.FullName,
            VehiclePlateNo = t.Vehicle?.PlateNo,
            BranchName = t.Branch?.Name
        };
    }

    private static bool IsValidStatusTransition(OpTaskStatus from, OpTaskStatus to)
    {
        return (from, to) switch
        {
            (OpTaskStatus.New, OpTaskStatus.Assigned) => true,
            (OpTaskStatus.New, OpTaskStatus.Cancelled) => true,
            (OpTaskStatus.Assigned, OpTaskStatus.InProgress) => true,
            (OpTaskStatus.Assigned, OpTaskStatus.Cancelled) => true,
            (OpTaskStatus.InProgress, OpTaskStatus.Completed) => true,
            (OpTaskStatus.InProgress, OpTaskStatus.Failed) => true,
            _ => false
        };
    }
}
