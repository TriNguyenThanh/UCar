using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.SurchargePolicy;

namespace UCar.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class SurchargePolicyController : Controller
{
    private readonly ISurchargePolicyService _surchargePolicyService;
    private readonly UCarDbContext _context;

    public SurchargePolicyController(ISurchargePolicyService surchargePolicyService, UCarDbContext context)
    {
        _surchargePolicyService = surchargePolicyService;
        _context = context;
    }

    // GET: SurchargePolicy
    public async Task<IActionResult> Index(SurchargeType? type = null, bool? isActive = null, int page = 1, int pageSize = 24)
    {
        var result = await _surchargePolicyService.GetSurchargePoliciesAsync(type, null, isActive, page, pageSize);
        
        ViewBag.CurrentType = type;
        ViewBag.CurrentActive = isActive;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.TotalCount = result.TotalCount;
        
        return View(result.Items);
    }

    // GET: SurchargePolicy/Create
    public async Task<IActionResult> Create()
    {
        await LoadVehicleTypes();
        return View(new SurchargePolicyViewModel());
    }

    // POST: SurchargePolicy/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SurchargePolicyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadVehicleTypes();
            return View(model);
        }

        var policy = new Models.SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = model.PolicyName,
            VehicleTypeId = model.VehicleTypeId,
            SurchargeType = model.SurchargeType,
            CalculationType = model.CalculationType,
            Value = model.Value,
            AppliesTo = model.AppliesTo,
            Unit = model.Unit,
            Description = model.Description,
            ValidFrom = model.ValidFrom,
            ValidUntil = model.ValidUntil,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now,
            CreatedBy = Guid.Empty // TODO: Get from logged user
        };

        var result = await _surchargePolicyService.CreateSurchargePolicyAsync(policy);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Tạo chính sách phụ phí thành công!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message ?? "Có lỗi xảy ra khi tạo chính sách");
        await LoadVehicleTypes();
        return View(model);
    }

    // GET: SurchargePolicy/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var policy = await _surchargePolicyService.GetSurchargePolicyByIdAsync(id);
        if (policy == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chính sách phụ phí";
            return RedirectToAction(nameof(Index));
        }

        var model = new SurchargePolicyViewModel
        {
            SurchargePolicyId = policy.SurchargePolicyId,
            PolicyName = policy.PolicyName,
            VehicleTypeId = policy.VehicleTypeId,
            SurchargeType = policy.SurchargeType,
            CalculationType = policy.CalculationType,
            Value = policy.Value,
            AppliesTo = policy.AppliesTo,
            Unit = policy.Unit,
            Description = policy.Description,
            ValidFrom = policy.ValidFrom,
            ValidUntil = policy.ValidUntil,
            IsActive = policy.IsActive,
            CreatedAt = policy.CreatedAt,
            ModifiedAt = policy.ModifiedAt
        };

        await LoadVehicleTypes();
        return View(model);
    }

    // POST: SurchargePolicy/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SurchargePolicyViewModel model)
    {
        if (id != model.SurchargePolicyId)
        {
            TempData["ErrorMessage"] = "ID không hợp lệ";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await LoadVehicleTypes();
            return View(model);
        }

        var policy = new Models.SurchargePolicy
        {
            SurchargePolicyId = model.SurchargePolicyId.Value,
            PolicyName = model.PolicyName,
            VehicleTypeId = model.VehicleTypeId,
            SurchargeType = model.SurchargeType,
            CalculationType = model.CalculationType,
            Value = model.Value,
            AppliesTo = model.AppliesTo,
            Unit = model.Unit,
            Description = model.Description,
            ValidFrom = model.ValidFrom,
            ValidUntil = model.ValidUntil,
            IsActive = model.IsActive,
            ModifiedAt = DateTime.Now,
            ModifiedBy = Guid.Empty // TODO: Get from logged user
        };

        var result = await _surchargePolicyService.UpdateSurchargePolicyAsync(id, policy);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Cập nhật chính sách phụ phí thành công!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message ?? "Có lỗi xảy ra khi cập nhật chính sách");
        await LoadVehicleTypes();
        return View(model);
    }

    // POST: SurchargePolicy/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _surchargePolicyService.DeleteSurchargePolicyAsync(id);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Xóa chính sách phụ phí thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Có lỗi xảy ra khi xóa chính sách";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: SurchargePolicy/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var policy = await _surchargePolicyService.GetSurchargePolicyByIdAsync(id);
        if (policy == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chính sách phụ phí";
            return RedirectToAction(nameof(Index));
        }

        var result = await _surchargePolicyService.SetSurchargePolicyActiveStatusAsync(id, !policy.IsActive);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Có lỗi xảy ra khi cập nhật trạng thái";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicleTypes()
    {
        ViewBag.VehicleTypes = await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .ToListAsync();
    }
}
