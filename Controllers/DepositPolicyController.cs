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
/// Controller quản lý chính sách đặt cọc
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
[Authorize(Roles = "Admin")]
public class DepositPolicyController : Controller
{
    private readonly IDepositPolicyService _depositPolicyService;
    private readonly UCarDbContext _context;
    private readonly ILogger<DepositPolicyController> _logger;

    public DepositPolicyController(
        IDepositPolicyService depositPolicyService,
        UCarDbContext context,
        ILogger<DepositPolicyController> logger)
    {
        _depositPolicyService = depositPolicyService;
        _context = context;
        _logger = logger;
    }

    #region Index - Danh sách chính sách đặt cọc

    /// <summary>
    /// Hiển thị danh sách chính sách đặt cọc
    /// GET: /DepositPolicy/Index
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(DepositPolicySearchViewModel searchModel)
    {
        try
        {
            var result = await _depositPolicyService.GetDepositPoliciesAsync(
                searchModel.VehicleTypeId,
                searchModel.IsActive,
                searchModel.PageNumber,
                searchModel.PageSize);

            // Map to ListViewModel
            var viewModels = result.Items.Select(dp => new DepositPolicyListViewModel
            {
                DepositPolicyId = dp.DepositPolicyId,
                PolicyName = dp.PolicyName,
                VehicleTypeName = dp.VehicleType.TypeName,
                VehicleTypeId = dp.VehicleTypeId,
                CalculationType = dp.CalculationType,
                Value = dp.Value,
                MinimumAmount = dp.MinimumAmount,
                MaximumAmount = dp.MaximumAmount,
                RefundProcessingDays = dp.RefundProcessingDays,
                ValidFrom = dp.ValidFrom,
                ValidTo = dp.ValidTo,
                IsActive = dp.IsActive
            }).ToList();

            var pagedList = new PaginatedList<DepositPolicyListViewModel>(
                viewModels,
                result.TotalCount,
                searchModel.PageNumber,
                searchModel.PageSize);

            // Prepare dropdowns for filter
            await PrepareFilterDropdowns();
            ViewData["CurrentVehicleTypeFilter"] = searchModel.VehicleTypeId;
            ViewData["CurrentActiveFilter"] = searchModel.IsActive;

            return View(pagedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deposit policy list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách chính sách đặt cọc";
            return View(new PaginatedList<DepositPolicyListViewModel>(
                new List<DepositPolicyListViewModel>(), 0, 1, 20));
        }
    }

    #endregion

    #region Create - Tạo chính sách đặt cọc mới

    /// <summary>
    /// Form tạo chính sách đặt cọc mới
    /// GET: /DepositPolicy/Create
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareFormDropdowns();
        return View(new DepositPolicyCreateViewModel());
    }

    /// <summary>
    /// Xử lý tạo chính sách đặt cọc mới
    /// POST: /DepositPolicy/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepositPolicyCreateViewModel model)
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

            // Validate MinimumAmount <= MaximumAmount
            if (model.MinimumAmount > model.MaximumAmount)
            {
                ModelState.AddModelError("MaximumAmount", "Số tiền tối đa phải >= số tiền tối thiểu");
                await PrepareFormDropdowns();
                return View(model);
            }

            var policy = new DepositPolicy
            {
                PolicyName = model.PolicyName,
                VehicleTypeId = model.VehicleTypeId,
                CalculationType = model.CalculationType,
                Value = model.Value,
                MinimumAmount = model.MinimumAmount,
                MaximumAmount = model.MaximumAmount,
                FullRefundCondition = model.FullRefundCondition,
                PartialRefundCondition = model.PartialRefundCondition,
                NoRefundCondition = model.NoRefundCondition,
                RefundProcessingDays = model.RefundProcessingDays,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = model.IsActive,
                Description = model.Description
            };

            var result = await _depositPolicyService.CreateDepositPolicyAsync(policy);

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
            _logger.LogError(ex, "Error creating deposit policy");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo chính sách đặt cọc";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Edit - Chỉnh sửa chính sách đặt cọc

    /// <summary>
    /// Form chỉnh sửa chính sách đặt cọc
    /// GET: /DepositPolicy/Edit/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var policy = await _depositPolicyService.GetDepositPolicyByIdAsync(id);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chính sách đặt cọc";
                return RedirectToAction(nameof(Index));
            }

            var model = new DepositPolicyEditViewModel
            {
                DepositPolicyId = policy.DepositPolicyId,
                PolicyName = policy.PolicyName,
                VehicleTypeId = policy.VehicleTypeId,
                CalculationType = policy.CalculationType,
                Value = policy.Value,
                MinimumAmount = policy.MinimumAmount,
                MaximumAmount = policy.MaximumAmount,
                FullRefundCondition = policy.FullRefundCondition,
                PartialRefundCondition = policy.PartialRefundCondition,
                NoRefundCondition = policy.NoRefundCondition,
                RefundProcessingDays = policy.RefundProcessingDays,
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
            _logger.LogError(ex, "Error loading deposit policy for edit: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Xử lý chỉnh sửa chính sách đặt cọc
    /// POST: /DepositPolicy/Edit/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DepositPolicyEditViewModel model)
    {
        if (id != model.DepositPolicyId)
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

            // Validate MinimumAmount <= MaximumAmount
            if (model.MinimumAmount > model.MaximumAmount)
            {
                ModelState.AddModelError("MaximumAmount", "Số tiền tối đa phải >= số tiền tối thiểu");
                await PrepareFormDropdowns();
                return View(model);
            }

            var policy = new DepositPolicy
            {
                DepositPolicyId = model.DepositPolicyId,
                PolicyName = model.PolicyName,
                VehicleTypeId = model.VehicleTypeId,
                CalculationType = model.CalculationType,
                Value = model.Value,
                MinimumAmount = model.MinimumAmount,
                MaximumAmount = model.MaximumAmount,
                FullRefundCondition = model.FullRefundCondition,
                PartialRefundCondition = model.PartialRefundCondition,
                NoRefundCondition = model.NoRefundCondition,
                RefundProcessingDays = model.RefundProcessingDays,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = model.IsActive,
                Description = model.Description
            };

            var result = await _depositPolicyService.UpdateDepositPolicyAsync(id, policy);

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
            _logger.LogError(ex, "Error updating deposit policy: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật chính sách";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Details - Chi tiết chính sách đặt cọc

    /// <summary>
    /// Hiển thị chi tiết chính sách đặt cọc
    /// GET: /DepositPolicy/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var policy = await _depositPolicyService.GetDepositPolicyByIdAsync(id);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chính sách đặt cọc";
                return RedirectToAction(nameof(Index));
            }

            var model = new DepositPolicyDetailsViewModel
            {
                DepositPolicyId = policy.DepositPolicyId,
                PolicyName = policy.PolicyName,
                VehicleTypeName = policy.VehicleType.TypeName,
                VehicleTypeId = policy.VehicleTypeId,
                CalculationType = policy.CalculationType,
                Value = policy.Value,
                MinimumAmount = policy.MinimumAmount,
                MaximumAmount = policy.MaximumAmount,
                FullRefundCondition = policy.FullRefundCondition,
                PartialRefundCondition = policy.PartialRefundCondition,
                NoRefundCondition = policy.NoRefundCondition,
                RefundProcessingDays = policy.RefundProcessingDays,
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
            _logger.LogError(ex, "Error loading deposit policy details: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region Delete - Xóa chính sách đặt cọc

    /// <summary>
    /// Xóa chính sách đặt cọc
    /// POST: /DepositPolicy/Delete/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _depositPolicyService.DeleteDepositPolicyAsync(id);

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
            _logger.LogError(ex, "Error deleting deposit policy: {PolicyId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa chính sách";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region ToggleActive - Bật/tắt trạng thái

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của chính sách
    /// POST: /DepositPolicy/ToggleActive/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive)
    {
        try
        {
            var result = await _depositPolicyService.SetDepositPolicyActiveStatusAsync(id, isActive);

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
            _logger.LogError(ex, "Error toggling deposit policy active status: {PolicyId}", id);
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
