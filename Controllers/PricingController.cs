using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.ViewModels.Pricing;
using System.Security.Claims;

namespace UCar.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class PricingController : Controller
{
    private readonly IPricingService _pricingService;
    private readonly IVehicleCatalogService _vehicleCatalogService;

    public PricingController(
        IPricingService pricingService,
        IVehicleCatalogService vehicleCatalogService)
    {
        _pricingService = pricingService;
        _vehicleCatalogService = vehicleCatalogService;
    }

    // GET: Pricing
    public async Task<IActionResult> Index(Guid? vehicleModelId, bool? isActive, int page = 1)
    {
        var result = await _pricingService.GetPricesAsync(vehicleModelId, isActive, page, 20);
        
        // Prepare filter dropdowns
        var models = await _vehicleCatalogService.GetAllVehicleModelsAsync();
        ViewBag.VehicleModels = new SelectList(models, "ModelId", "ModelName");
        ViewBag.CurrentVehicleModelId = vehicleModelId;
        ViewBag.CurrentIsActive = isActive;
        
        return View(result);
    }

    // GET: Pricing/Create
    public async Task<IActionResult> Create()
    {
        await PrepareViewBagAsync();
        
        var model = new PricingCreateViewModel
        {
            IsActive = true,
            MonthlyMultiplier = 0.85m,
            HolidayMultiplier = 1.30m,
            WeekendMultiplier = 1.15m,
            OvertimeHourlyPrice = 50_000m,
            DepositSuggest = 5_000_000m
        };
        
        return View(model);
    }

    // POST: Pricing/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PricingCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PrepareViewBagAsync();
            return View(viewModel);
        }

        // Map ViewModel to Price entity
        var price = new Price
        {
            VehicleModelId = viewModel.VehicleModelId,
            Name = viewModel.Name,
            BaseDailyPrice = viewModel.DailyBasePrice,
            MonthMultiplier = viewModel.MonthlyMultiplier,
            PeakMultiplier = viewModel.HolidayMultiplier,
            OvertimeHourlyPrice = viewModel.OvertimeHourlyPrice,
            ValidFrom = DateTime.Now,
            IsActive = viewModel.IsActive
        };

        var result = await _pricingService.CreatePriceAsync(price);
        
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Errors.FirstOrDefault() ?? "Lỗi khi tạo bảng giá");
            await PrepareViewBagAsync();
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Tạo bảng giá thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Pricing/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var price = await _pricingService.GetPriceByIdAsync(id);
        if (price == null)
        {
            return NotFound();
        }

        // Map Price entity to ViewModel
        var viewModel = new PricingEditViewModel
        {
            PriceId = price.PriceId,
            VehicleModelId = price.VehicleModelId,
            Name = price.Name,
            DailyBasePrice = price.BaseDailyPrice,
            MonthlyMultiplier = price.MonthMultiplier,
            HolidayMultiplier = price.PeakMultiplier,
            WeekendMultiplier = 1.15m, // Default if not stored
            OvertimeHourlyPrice = price.OvertimeHourlyPrice,
            DepositSuggest = 5_000_000m, // Default if not stored
            IsActive = price.IsActive
        };

        await PrepareViewBagAsync(price.VehicleModelId);
        return View(viewModel);
    }

    // POST: Pricing/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PricingEditViewModel viewModel)
    {
        if (id != viewModel.PriceId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PrepareViewBagAsync(viewModel.VehicleModelId);
            return View(viewModel);
        }

        // Map ViewModel back to Price entity
        var price = new Price
        {
            PriceId = viewModel.PriceId,
            VehicleModelId = viewModel.VehicleModelId,
            Name = viewModel.Name,
            BaseDailyPrice = viewModel.DailyBasePrice,
            MonthMultiplier = viewModel.MonthlyMultiplier,
            PeakMultiplier = viewModel.HolidayMultiplier,
            OvertimeHourlyPrice = viewModel.OvertimeHourlyPrice,
            IsActive = viewModel.IsActive
        };

        var result = await _pricingService.UpdatePriceAsync(id, price);
        
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Errors.FirstOrDefault() ?? "Lỗi khi cập nhật bảng giá");
            await PrepareViewBagAsync(viewModel.VehicleModelId);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Cập nhật bảng giá thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Pricing/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var price = await _pricingService.GetPriceByIdAsync(id);
        if (price == null)
        {
            return NotFound();
        }

        return View(price);
    }

    // POST: Pricing/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _pricingService.DeletePriceAsync(id);
        
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Không thể xóa bảng giá";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Xóa bảng giá thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Pricing/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive)
    {
        var result = await _pricingService.SetPriceActiveStatusAsync(id, isActive);
        
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Không thể cập nhật trạng thái";
        }
        else
        {
            TempData["SuccessMessage"] = result.Message ?? "Cập nhật trạng thái thành công!";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PrepareViewBagAsync(Guid? selectedModelId = null)
    {
        var models = await _vehicleCatalogService.GetAllVehicleModelsAsync();
        ViewBag.VehicleModels = new SelectList(
            models.Select(m => new
            {
                m.ModelId,
                DisplayName = $"{m.Make} {m.ModelName} ({m.VehicleTypeName})"
            }),
            "ModelId",
            "DisplayName",
            selectedModelId
        );
    }
}
