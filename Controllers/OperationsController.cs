using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCar.Interfaces;
using UCar.Models.DTOs.Operations;
using UCar.Models.Enums;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý vận hành (chi nhánh, nhiệm vụ, ca làm)
/// Tương ứng DFD 8.1, 8.2, 8.3
/// </summary>
public class OperationsController : Controller
{
    private readonly IBranchService _branchService;
    private readonly IOperationalTaskService _taskService;
    private readonly IShiftService _shiftService;
    private readonly IStaffService _staffService;
    private readonly IVehicleService _vehicleService;

    public OperationsController(
        IBranchService branchService,
        IOperationalTaskService taskService,
        IShiftService shiftService,
        IStaffService staffService,
        IVehicleService vehicleService)
    {
        _branchService = branchService;
        _taskService = taskService;
        _shiftService = shiftService;
        _staffService = staffService;
        _vehicleService = vehicleService;
    }

    /// <summary>Lấy User ID từ authentication cookie</summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }

    #region Branches (8.1)

    public async Task<IActionResult> Branches()
    {
        var branches = await _branchService.GetAllAsync();
        return View(branches);
    }

    public IActionResult CreateBranch()
    {
        return View(new BranchCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBranch(BranchCreateDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _branchService.CreateAsync(dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Branches));
    }

    public async Task<IActionResult> EditBranch(Guid id)
    {
        var branch = await _branchService.GetByIdAsync(id);
        if (branch == null)
            return NotFound();

        var dto = new BranchUpdateDto
        {
            Name = branch.Name,
            Address = branch.Address,
            PhoneContact = branch.PhoneContact
        };

        ViewBag.BranchId = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBranch(Guid id, BranchUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BranchId = id;
            return View(dto);
        }

        var result = await _branchService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.BranchId = id;
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Branches));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        var result = await _branchService.DeleteAsync(id);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(Branches));
    }

    #endregion

    #region Tasks (8.2)

    public async Task<IActionResult> Tasks(TaskFilterDto filter)
    {
        var result = await _taskService.GetTasksAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.Staff = await _staffService.GetAvailableStaffAsync();
        return View(result);
    }

    public async Task<IActionResult> TaskDetails(Guid id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        ViewBag.Staff = await _staffService.GetAvailableStaffAsync(task.BranchId);
        return View(task);
    }

    public async Task<IActionResult> CreateTask()
    {
        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.Staff = await _staffService.GetAvailableStaffAsync();
        ViewBag.Vehicles = await _vehicleService.GetAvailableVehiclesAsync();
        return View(new TaskCreateDto { ScheduledAt = DateTime.Now.AddHours(1) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(TaskCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetAllAsync();
            ViewBag.Staff = await _staffService.GetAvailableStaffAsync();
            ViewBag.Vehicles = await _vehicleService.GetAvailableVehiclesAsync();
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _taskService.CreateTaskAsync(dto, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.Branches = await _branchService.GetAllAsync();
            ViewBag.Staff = await _staffService.GetAvailableStaffAsync();
            ViewBag.Vehicles = await _vehicleService.GetAvailableVehiclesAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Tasks));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(Guid taskId, Guid staffId)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.AssignTaskAsync(taskId, staffId, userId);

        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(TaskDetails), new { id = taskId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(Guid taskId, TaskUpdateStatusDto dto)
    {
        var result = await _taskService.UpdateStatusAsync(taskId, dto);

        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(TaskDetails), new { id = taskId });
    }

    #endregion

    #region Shifts (8.3)

    public async Task<IActionResult> Shifts(Guid? branchId = null)
    {
        var shifts = await _shiftService.GetShiftsAsync(branchId);
        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.SelectedBranchId = branchId;
        return View(shifts);
    }

    public async Task<IActionResult> CreateShift()
    {
        ViewBag.Branches = await _branchService.GetAllAsync();
        return View(new ShiftCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShift(ShiftCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        var result = await _shiftService.CreateShiftAsync(dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Shifts));
    }

    public async Task<IActionResult> EditShift(Guid id)
    {
        var shift = await _shiftService.GetByIdAsync(id);
        if (shift == null)
            return NotFound();

        var dto = new ShiftUpdateDto
        {
            ShiftName = shift.ShiftName,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            BranchId = shift.BranchId,
            Description = shift.Description,
            IsActive = shift.IsActive
        };

        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.ShiftId = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditShift(Guid id, ShiftUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetAllAsync();
            ViewBag.ShiftId = id;
            return View(dto);
        }

        var result = await _shiftService.UpdateShiftAsync(id, dto);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.Branches = await _branchService.GetAllAsync();
            ViewBag.ShiftId = id;
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Shifts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShift(Guid id)
    {
        var result = await _shiftService.DeleteShiftAsync(id);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(Shifts));
    }

    #endregion

    #region Shift Schedule (8.3)

    public async Task<IActionResult> ShiftSchedule(ShiftScheduleFilterDto filter)
    {
        filter.DateFrom ??= DateOnly.FromDateTime(DateTime.Today);
        filter.DateTo ??= filter.DateFrom.Value.AddDays(13);

        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.Shifts = await _shiftService.GetShiftsAsync(filter.BranchId);
        ViewBag.Staff = await _staffService.GetAvailableStaffAsync(filter.BranchId);
        ViewBag.Filter = filter;

        return View();
    }

    /// <summary>API endpoint for FullCalendar</summary>
    [HttpGet]
    public async Task<IActionResult> GetCalendarEvents([FromQuery] ShiftScheduleFilterDto filter)
    {
        var events = await _shiftService.GetCalendarEventsAsync(filter);
        return Json(events);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignShift(ShiftAssignmentCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _shiftService.AssignShiftAsync(dto, userId);

        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(ShiftSchedule));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveShiftAssignment(Guid id)
    {
        var result = await _shiftService.RemoveAssignmentAsync(id);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(ShiftSchedule));
    }

    #endregion
}
