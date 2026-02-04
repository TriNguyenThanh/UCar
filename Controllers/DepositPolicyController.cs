using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.DepositPolicy;

namespace UCar.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class DepositPolicyController : Controller
{
    private readonly IDepositPolicyService _depositPolicyService;
    private readonly UCarDbContext _context;

    public DepositPolicyController(
        IDepositPolicyService depositPolicyService,
        UCarDbContext context)
    {
        _depositPolicyService = depositPolicyService;
        _context = context;
    }

    // GET: DepositPolicy/Index
    public async Task<IActionResult> Index(bool? isActive, int page = 1)
    {
        var result = await _depositPolicyService.GetDepositPoliciesAsync(
            vehicleModelId: null,
            isActive: isActive,
            page: page,
            pageSize: 20);

        ViewBag.IsActiveFilter = isActive;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

        return View(result.Items);
    }

    // GET: DepositPolicy/Create
    public async Task<IActionResult> Create()
    {
        await LoadVehicleTypes();
        
        var model = new DepositPolicyViewModel
        {
            ValidFrom = DateTime.Today,
            IsActive = true,
            RefundProcessingDays = 15,
            ResponsibilityDepositAmount = 2_000_000,
            RentalDepositCalculationType = DepositCalculationType.Percentage,
            RentalDepositValue = 50,
            RentalDepositMinimum = 2_000_000,
            RentalDepositMaximum = 10_000_000
        };

        return View(model);
    }

    // POST: DepositPolicy/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepositPolicyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadVehicleTypes();
            return View(model);
        }

        var policy = new DepositPolicy
        {
            PolicyName = model.PolicyName,
            VehicleTypeId = model.VehicleTypeId,
            ResponsibilityDepositAmount = model.ResponsibilityDepositAmount,
            RentalDepositCalculationType = model.RentalDepositCalculationType,
            RentalDepositValue = model.RentalDepositValue,
            RentalDepositMinimum = model.RentalDepositMinimum,
            RentalDepositMaximum = model.RentalDepositMaximum,
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
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message ?? "Có lỗi xảy ra");
        await LoadVehicleTypes();
        return View(model);
    }

    // GET: DepositPolicy/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var policy = await _depositPolicyService.GetDepositPolicyByIdAsync(id);
        
        if (policy == null)
        {
            return NotFound();
        }

        await LoadVehicleTypes();

        var model = new DepositPolicyViewModel
        {
            DepositPolicyId = policy.DepositPolicyId,
            PolicyName = policy.PolicyName,
            VehicleTypeId = policy.VehicleTypeId,
            ResponsibilityDepositAmount = policy.ResponsibilityDepositAmount,
            RentalDepositCalculationType = policy.RentalDepositCalculationType,
            RentalDepositValue = policy.RentalDepositValue,
            RentalDepositMinimum = policy.RentalDepositMinimum,
            RentalDepositMaximum = policy.RentalDepositMaximum,
            FullRefundCondition = policy.FullRefundCondition,
            PartialRefundCondition = policy.PartialRefundCondition,
            NoRefundCondition = policy.NoRefundCondition,
            RefundProcessingDays = policy.RefundProcessingDays,
            ValidFrom = policy.ValidFrom,
            ValidTo = policy.ValidTo,
            IsActive = policy.IsActive,
            Description = policy.Description,
            VehicleTypeName = policy.VehicleType?.TypeName,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };

        return View(model);
    }

    // POST: DepositPolicy/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DepositPolicyViewModel model)
    {
        if (id != model.DepositPolicyId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadVehicleTypes();
            return View(model);
        }

        var policy = new DepositPolicy
        {
            DepositPolicyId = model.DepositPolicyId,
            PolicyName = model.PolicyName,
            VehicleTypeId = model.VehicleTypeId,
            ResponsibilityDepositAmount = model.ResponsibilityDepositAmount,
            RentalDepositCalculationType = model.RentalDepositCalculationType,
            RentalDepositValue = model.RentalDepositValue,
            RentalDepositMinimum = model.RentalDepositMinimum,
            RentalDepositMaximum = model.RentalDepositMaximum,
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
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message ?? "Có lỗi xảy ra");
        await LoadVehicleTypes();
        return View(model);
    }

    // POST: DepositPolicy/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
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

    // POST: DepositPolicy/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive)
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

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicleTypes()
    {
        var vehicleTypes = await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .Select(vt => new { vt.VehicleTypeId, vt.TypeName })
            .ToListAsync();

        ViewBag.VehicleTypes = new SelectList(vehicleTypes, "VehicleTypeId", "TypeName");
    }
}
