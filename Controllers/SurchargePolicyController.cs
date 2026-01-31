using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.ViewModels;
using UCar.ViewModels.Pricing;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý chính sách phụ phí
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
[Authorize(Roles = "Admin")]
public class SurchargePolicyController : Controller
{
    private readonly ISurchargePolicyService _surchargePolicyService;
    private readonly UCarDbContext _context;
    private readonly ILogger<SurchargePolicyController> _logger;

    public SurchargePolicyController(
        ISurchargePolicyService surchargePolicyService,
        UCarDbContext context,
        ILogger<SurchargePolicyController> logger)
    {
        _surchargePolicyService = surchargePolicyService;
        _context = context;
        _logger = logger;
    }

    #region Index - Danh sách chính sách phụ phí

    /// <summary>
    /// Hiển thị danh sách chính sách phụ phí
    /// GET: /SurchargePolicy/Index
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(SurchargePolicySearchViewModel searchModel)
    {
        try
        {
            var result = await _surchargePolicyService.GetSurchargePoliciesAsync(
                searchModel.Type,
                searchModel.VehicleTypeId,
                searchModel.IsActive,
                searchModel.PageNumber,
                searchModel.PageSize);

            // Map to ListViewModel
            var viewModels = result.Items.Select(sp => new SurchargePolicyListViewModel
            {
                SurchargePolicyId = sp.SurchargePolicyId,
                PolicyName = sp.PolicyName,
                Type = sp.Type,
                VehicleTypeName = sp.VehicleType?.TypeName,
                VehicleTypeId = sp.VehicleTypeId,
                CalculationType = sp.CalculationType,
                Value = sp.Value,
                MinimumAmount = sp.MinimumAmount,
                MaximumAmount = sp.MaximumAmount,
                Priority = sp.Priority,
                ValidFrom = sp.ValidFrom,
                ValidTo = sp.ValidTo,
                IsActive = sp.IsActive
            }).ToList();

            var pagedList = new PaginatedList<SurchargePolicyListViewModel>(
                viewModels,
                result.TotalCount,
                searchModel.PageNumber,
                searchModel.PageSize);

            // Prepare dropdowns for filter
            await PrepareFilterDropdowns();
            ViewData["CurrentTypeFilter"] = searchModel.Type;
            ViewData["CurrentVehicleTypeFilter"] = searchModel.VehicleTypeId;
            ViewData["CurrentActiveFilter"] = searchModel.IsActive;

            return View(pagedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading surcharge policy list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách chính sách phụ phí";
            return View(new PaginatedList<SurchargePolicyListViewModel>(
                new List<SurchargePolicyListViewModel>(), 0, 1, 20));
        }
    }

    #endregion

    #region Create - Tạo chính sách phụ phí mới

    /// <summary>
    /// Form tạo chính sách phụ phí mới
    /// GET: /SurchargePolicy/Create
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareFormDropdowns();
        return View(new SurchargePolicyCreateViewModel());
    }

    /// <summary>
    /// Xử lý tạo chính sách phụ phí mới
    /// POST: /SurchargePolicy/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SurchargePolicyCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PrepareFormDropdowns();
            return View(model);
        }

        try
        {
            // Validate ValidTo >= ValidFrom
            if (model.ValidTo.HasValue && model.ValidTo.Value < model.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Ngày kết thúc hiệu lực phải >= ngày bắt đầu");
                await PrepareFormDropdowns();
                return View(model);
            }

            // Validate MinimumAmount <= MaximumAmount if both provided
            if (model.MinimumAmount.HasValue && model.MaximumAmount.HasValue 
                && model.MinimumAmount.Value > model.MaximumAmount.Value)
            {
                ModelState.AddModelError("MaximumAmount", "Số tiền tối đa phải >= số tiền tối thiểu");
                await PrepareFormDropdowns();
                return View(model);
            }

            var policy = new SurchargePolicy
            {
                PolicyName = model.PolicyName,
                Type = model.Type,
                VehicleTypeId = model.VehicleTypeId,
                CalculationType = model.CalculationType,
                Value = model.Value,
                MinimumAmount = model.MinimumAmount,
                MaximumAmount = model.MaximumAmount,
                ApplicableCondition = model.ApplicableCondition,
                Priority = model.Priority,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = model.IsActive,
                Description = model.Description
            };

            var result = await _surchargePolicyService.CreateSurchargePolicyAsync(policy);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = result.Data });
            }

            TempData["ErrorMessage"] = result.Message;
            await PrepareFormDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating surcharge policy");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo chính sách phụ phí";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Edit - Chỉnh sửa chính sách phụ phí

    /// <summary>
    /// Form chỉnh sửa chính sách phụ phí
    /// GET: /SurchargePolicy/Edit/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var policy = await _surchargePolicyService.GetSurchargePolicyByIdAsync(id);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chính sách phụ phí";
                return RedirectToAction(nameof(Index));
            }

            var model = new SurchargePolicyEditViewModel
            {
                SurchargePolicyId = policy.SurchargePolicyId,
                PolicyName = policy.PolicyName,
                Type = policy.Type,
                VehicleTypeId = policy.VehicleTypeId,
                CalculationType = policy.CalculationType,
                Value = policy.Value,
                MinimumAmount = policy.MinimumAmount,
                MaximumAmount = policy.MaximumAmount,
                ApplicableCondition = policy.ApplicableCondition,
                Priority = policy.Priority,
                ValidFrom = policy.ValidFrom,
                ValidTo = policy.ValidTo,
                IsActive = policy.IsActive,
                Description = policy.Description
            };

            await PrepareFormDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading surcharge policy for edit: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Xử lý chỉnh sửa chính sách phụ phí
    /// POST: /SurchargePolicy/Edit/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SurchargePolicyEditViewModel model)
    {
        if (id != model.SurchargePolicyId)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await PrepareFormDropdowns();
            return View(model);
        }

        try
        {
            // Validate ValidTo >= ValidFrom
            if (model.ValidTo.HasValue && model.ValidTo.Value < model.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Ngày kết thúc hiệu lực phải >= ngày bắt đầu");
                await PrepareFormDropdowns();
                return View(model);
            }

            // Validate MinimumAmount <= MaximumAmount if both provided
            if (model.MinimumAmount.HasValue && model.MaximumAmount.HasValue 
                && model.MinimumAmount.Value > model.MaximumAmount.Value)
            {
                ModelState.AddModelError("MaximumAmount", "Số tiền tối đa phải >= số tiền tối thiểu");
                await PrepareFormDropdowns();
                return View(model);
            }

            var policy = new SurchargePolicy
            {
                SurchargePolicyId = model.SurchargePolicyId,
                PolicyName = model.PolicyName,
                Type = model.Type,
                VehicleTypeId = model.VehicleTypeId,
                CalculationType = model.CalculationType,
                Value = model.Value,
                MinimumAmount = model.MinimumAmount,
                MaximumAmount = model.MaximumAmount,
                ApplicableCondition = model.ApplicableCondition,
                Priority = model.Priority,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = model.IsActive,
                Description = model.Description
            };

            var result = await _surchargePolicyService.UpdateSurchargePolicyAsync(id, policy);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["ErrorMessage"] = result.Message;
            await PrepareFormDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating surcharge policy: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật chính sách";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Details - Chi tiết chính sách phụ phí

    /// <summary>
    /// Hiển thị chi tiết chính sách phụ phí
    /// GET: /SurchargePolicy/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var policy = await _surchargePolicyService.GetSurchargePolicyByIdAsync(id);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chính sách phụ phí";
                return RedirectToAction(nameof(Index));
            }

            var model = new SurchargePolicyDetailsViewModel
            {
                SurchargePolicyId = policy.SurchargePolicyId,
                PolicyName = policy.PolicyName,
                Type = policy.Type,
                VehicleTypeName = policy.VehicleType?.TypeName,
                VehicleTypeId = policy.VehicleTypeId,
                CalculationType = policy.CalculationType,
                Value = policy.Value,
                MinimumAmount = policy.MinimumAmount,
                MaximumAmount = policy.MaximumAmount,
                ApplicableCondition = policy.ApplicableCondition,
                Priority = policy.Priority,
                ValidFrom = policy.ValidFrom,
                ValidTo = policy.ValidTo,
                IsActive = policy.IsActive,
                Description = policy.Description,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading surcharge policy details: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region Delete - Xóa chính sách phụ phí

    /// <summary>
    /// Xóa chính sách phụ phí
    /// POST: /SurchargePolicy/Delete/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _surchargePolicyService.DeleteSurchargePolicyAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting surcharge policy: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region ToggleActive - Bật/tắt trạng thái

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của chính sách
    /// POST: /SurchargePolicy/ToggleActive/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive)
    {
        try
        {
            var result = await _surchargePolicyService.SetSurchargePolicyActiveStatusAsync(id, isActive);

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
            _logger.LogError(ex, "Error toggling surcharge policy active status: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Chuẩn bị dropdowns cho form create/edit
    /// </summary>
    private async Task PrepareFormDropdowns()
    {
        var vehicleTypes = await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .ToListAsync();

        ViewData["VehicleTypes"] = new SelectList(vehicleTypes, "VehicleTypeId", "TypeName");
    }

    /// <summary>
    /// Chuẩn bị dropdowns cho filter
    /// </summary>
    private async Task PrepareFilterDropdowns()
    {
        var vehicleTypes = await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .ToListAsync();

        ViewData["VehicleTypes"] = new SelectList(vehicleTypes, "VehicleTypeId", "TypeName");
    }

    #endregion
}
