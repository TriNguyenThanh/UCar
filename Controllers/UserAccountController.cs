using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models.Enums;
using UCar.ViewModels.UserAccount;

namespace UCar.Controllers;

/// <summary>
/// Quản lý tài khoản người dùng
/// </summary>
[Authorize(Roles = "Admin")]
public class UserAccountController : Controller
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<UserAccountController> _logger;

    public UserAccountController(
        IUserAccountService userAccountService,
        ILogger<UserAccountController> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    /// <summary>
    /// Danh sách tài khoản người dùng
    /// GET: /UserAccount
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        RoleCode? role,
        bool? isActive,
        string? searchTerm,
        int page = 1)
    {
        var (items, totalCount, totalPages) = await _userAccountService.GetUserAccountsAsync(
            role, isActive, searchTerm, page, 15);

        ViewBag.CurrentRole = role;
        ViewBag.CurrentIsActive = isActive;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    /// <summary>
    /// Chi tiết tài khoản
    /// GET: /UserAccount/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userAccountService.GetUserAccountDetailsAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản";
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    /// <summary>
    /// Form tạo tài khoản mới
    /// GET: /UserAccount/Create
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserAccountViewModel
        {
            AvailableRoles = await _userAccountService.GetRolesForSelectAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Xử lý tạo tài khoản
    /// POST: /UserAccount/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _userAccountService.GetRolesForSelectAsync();
            return View(model);
        }

        var (success, message, userId) = await _userAccountService.CreateUserAccountAsync(model);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        TempData["ErrorMessage"] = message;
        model.AvailableRoles = await _userAccountService.GetRolesForSelectAsync();
        return View(model);
    }

    /// <summary>
    /// Form chỉnh sửa tài khoản
    /// GET: /UserAccount/Edit/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _userAccountService.GetUserAccountForEditAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý cập nhật tài khoản
    /// POST: /UserAccount/Edit
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _userAccountService.GetRolesForSelectAsync();
            return View(model);
        }

        var (success, message) = await _userAccountService.UpdateUserAccountAsync(model);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = model.UserId });
        }

        TempData["ErrorMessage"] = message;
        model.AvailableRoles = await _userAccountService.GetRolesForSelectAsync();
        return View(model);
    }

    /// <summary>
    /// Khóa/Mở khóa tài khoản
    /// POST: /UserAccount/ToggleStatus/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var (success, message) = await _userAccountService.ToggleAccountStatusAsync(id);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Form đặt lại mật khẩu
    /// GET: /UserAccount/ResetPassword/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _userAccountService.GetUserAccountDetailsAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản";
            return RedirectToAction(nameof(Index));
        }

        var model = new ResetPasswordViewModel
        {
            UserId = user.UserId,
            Username = user.Username
        };

        return View(model);
    }

    /// <summary>
    /// Xử lý đặt lại mật khẩu
    /// POST: /UserAccount/ResetPassword
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, message) = await _userAccountService.ResetPasswordAsync(model.UserId, model.NewPassword);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = model.UserId });
        }

        TempData["ErrorMessage"] = message;
        return View(model);
    }

    /// <summary>
    /// API kiểm tra username tồn tại
    /// GET: /UserAccount/CheckUsername
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username, Guid? userId)
    {
        var exists = await _userAccountService.IsUsernameExistsAsync(username, userId);
        return Json(new { exists });
    }

    /// <summary>
    /// API kiểm tra email tồn tại
    /// GET: /UserAccount/CheckEmail
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email, Guid? userId)
    {
        var exists = await _userAccountService.IsEmailExistsAsync(email, userId);
        return Json(new { exists });
    }
}
