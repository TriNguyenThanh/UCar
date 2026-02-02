using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCar.Interfaces;
using UCar.Models.DTOs.Operations;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý nhân viên
/// Tương ứng DFD 8.3 - Quản lý nhân sự
/// </summary>
[Authorize(Roles = "Admin,BranchManager")]
public class StaffController : Controller
{
    private readonly IStaffService _staffService;
    private readonly IBranchService _branchService;
    private readonly IBranchAccessService _branchAccess;

    public StaffController(IStaffService staffService, IBranchService branchService, IBranchAccessService branchAccess)
    {
        _staffService = staffService;
        _branchService = branchService;
        _branchAccess = branchAccess;
    }

    /// <summary>Lấy User ID từ authentication cookie</summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }


    /// <summary>Danh sách nhân viên</summary>
    public async Task<IActionResult> Index(StaffFilterDto filter)
    {
        // Auto-set BranchId for BranchManager
        var userBranchId = _branchAccess.GetCurrentUserBranchId();
        if (userBranchId.HasValue)
        {
            filter.BranchId = userBranchId.Value;
        }

        var result = await _staffService.GetStaffListAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.Branches = await _branchService.GetAllAsync();
        ViewBag.IsBranchManager = userBranchId.HasValue;
        return View(result);
    }

    /// <summary>Chi tiết nhân viên</summary>
    public async Task<IActionResult> Details(Guid id)
    {
        var staff = await _staffService.GetByIdAsync(id);
        if (staff == null)
            return NotFound();

        return View(staff);
    }

    /// <summary>Form tạo nhân viên</summary>
    public async Task<IActionResult> Create()
    {
        ViewBag.Branches = await _branchService.GetAllAsync();
        return View(new StaffCreateDto());
    }

    /// <summary>Submit tạo nhân viên</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _staffService.CreateStaffAsync(dto, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Form sửa nhân viên</summary>
    public async Task<IActionResult> Edit(Guid id)
    {
        var staff = await _staffService.GetByIdAsync(id);
        if (staff == null)
            return NotFound();

        var dto = new StaffUpdateDto
        {
            FullName = staff.FullName,
            Position = staff.Position,
            BranchId = staff.BranchId,
            Email = staff.Email,
            Phone = staff.Phone,
            IsActive = staff.IsActive
        };

        ViewBag.StaffId = id;
        ViewBag.StaffCode = staff.StaffCode;
        ViewBag.Username = staff.Username;
        ViewBag.Branches = await _branchService.GetAllAsync();
        return View(dto);
    }

    /// <summary>Submit sửa nhân viên</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StaffUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.StaffId = id;
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        var result = await _staffService.UpdateStaffAsync(id, dto);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            ViewBag.StaffId = id;
            ViewBag.Branches = await _branchService.GetAllAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Vô hiệu hóa nhân viên</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _staffService.DeactivateStaffAsync(id);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Kích hoạt lại nhân viên</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _staffService.ActivateStaffAsync(id);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Reset mật khẩu</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _staffService.ResetPasswordAsync(id, newPassword);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Errors.First();
        else
            TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>Xem hiệu suất nhân viên</summary>
    public async Task<IActionResult> Performance(Guid id, DateTime? from, DateTime? to)
    {
        from ??= DateTime.Today.AddDays(-30);
        to ??= DateTime.Today;

        var staff = await _staffService.GetByIdAsync(id);
        if (staff == null)
            return NotFound();

        var performance = await _staffService.GetPerformanceAsync(id, from.Value, to.Value);

        ViewBag.Staff = staff;
        ViewBag.DateFrom = from.Value;
        ViewBag.DateTo = to.Value;

        return View(performance);
    }
}
