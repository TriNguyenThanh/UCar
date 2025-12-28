using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.ViewModels;

namespace UCar.Controllers;

/// <summary>
/// Customer management controller
/// Implements customer management UI according to DFD 2.0
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        ICustomerService customerService,
        ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    #region DFD 2.2: Tra cứu lịch sử khách - Index/List

    /// <summary>
    /// Display customer list with search and filter
    /// GET: /Customer/Index
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CustomerSearchViewModel searchModel)
    {
        try
        {
            var customers = await _customerService.GetCustomersAsync(searchModel);
            ViewData["CurrentSearch"] = searchModel.SearchTerm;
            ViewData["CurrentStatusFilter"] = searchModel.StatusFilter;
            ViewData["CurrentRiskFilter"] = searchModel.RiskLevelFilter;
            ViewData["CurrentSort"] = searchModel.SortBy;
            
            return View(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách khách hàng";
            return View(new PaginatedList<CustomerListViewModel>(new List<CustomerListViewModel>(), 0, 1, 10));
        }
    }

    #endregion

    #region DFD 2.2: Tra cứu lịch sử khách - Details

    /// <summary>
    /// Display customer details with rental history
    /// GET: /Customer/Details/5
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var customer = await _customerService.GetCustomerDetailsAsync(id);
            
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Index));
            }

            // Check eligibility for rental
            var eligibility = await _customerService.CheckCustomerEligibilityAsync(id);
            ViewData["EligibilityStatus"] = eligibility.IsAllowed;
            ViewData["EligibilityMessage"] = eligibility.Message;

            return View(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer details: {CustomerId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin khách hàng";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region DFD 2.1: Đăng ký thông tin khách - Create

    /// <summary>
    /// Display create customer form
    /// GET: /Customer/Create
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerCreateViewModel());
    }

    /// <summary>
    /// Handle customer creation
    /// POST: /Customer/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional validations
            if (await _customerService.IsUsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            if (await _customerService.IsEmailExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View(model);
            }

            if (await _customerService.IsPhoneExistsAsync(model.Phone))
            {
                ModelState.AddModelError("Phone", "Số điện thoại đã được sử dụng");
                return View(model);
            }

            if (await _customerService.IsDocumentNumberExistsAsync(model.DocumentNumber))
            {
                ModelState.AddModelError("DocumentNumber", $"Số {model.DocumentType} đã được đăng ký");
                return View(model);
            }

            var result = await _customerService.CreateCustomerAsync(model);

            if (result.Success)
            {
                _logger.LogInformation("Customer created successfully: {CustomerId}", result.CustomerId);
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = result.CustomerId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo khách hàng");
            return View(model);
        }
    }

    #endregion

    #region Bổ sung hợp lý: Update Customer

    /// <summary>
    /// Display edit customer form
    /// GET: /Customer/Edit/5
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var customer = await _customerService.GetCustomerForEditAsync(id);
            
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer for edit: {CustomerId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin khách hàng";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Handle customer update
    /// POST: /Customer/Edit/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate unique constraints
            if (await _customerService.IsEmailExistsAsync(model.Email, model.CustomerId))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng bởi khách hàng khác");
                return View(model);
            }

            if (await _customerService.IsPhoneExistsAsync(model.Phone, model.CustomerId))
            {
                ModelState.AddModelError("Phone", "Số điện thoại đã được sử dụng bởi khách hàng khác");
                return View(model);
            }

            var result = await _customerService.UpdateCustomerAsync(model);

            if (result.Success)
            {
                _logger.LogInformation("Customer updated successfully: {CustomerId}", model.CustomerId);
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = model.CustomerId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", model.CustomerId);
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật khách hàng");
            return View(model);
        }
    }

    #endregion

    #region Bổ sung hợp lý: Toggle Status & Blacklist

    /// <summary>
    /// Toggle customer account status (active/inactive)
    /// POST: /Customer/ToggleStatus
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive)
    {
        try
        {
            var result = await _customerService.ToggleCustomerStatusAsync(id, isActive);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling customer status: {CustomerId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái tài khoản";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// Toggle customer blacklist status
    /// POST: /Customer/ToggleBlacklist
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlacklist(Guid id, bool isBlacklisted, string? reason)
    {
        try
        {
            var result = await _customerService.ToggleBlacklistAsync(id, isBlacklisted, reason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling blacklist: {CustomerId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái danh sách đen";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    #endregion

    #region AJAX Endpoints for Validation

    /// <summary>
    /// Check if username exists (for client-side validation)
    /// GET: /Customer/CheckUsername?username=test
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username, Guid? excludeId)
    {
        var exists = await _customerService.IsUsernameExistsAsync(username, excludeId);
        return Json(new { exists });
    }

    /// <summary>
    /// Check if email exists (for client-side validation)
    /// GET: /Customer/CheckEmail?email=test@test.com
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email, Guid? excludeId)
    {
        var exists = await _customerService.IsEmailExistsAsync(email, excludeId);
        return Json(new { exists });
    }

    /// <summary>
    /// Check if phone exists (for client-side validation)
    /// GET: /Customer/CheckPhone?phone=0123456789
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckPhone(string phone, Guid? excludeId)
    {
        var exists = await _customerService.IsPhoneExistsAsync(phone, excludeId);
        return Json(new { exists });
    }

    #endregion
}
