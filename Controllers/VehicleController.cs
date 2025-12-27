using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UCar.Interfaces;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý xe
/// </summary>
public class VehicleController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleCatalogService _catalogService;
    private readonly IVehicleStatusService _statusService;

    public VehicleController(
        IVehicleService vehicleService,
        IVehicleCatalogService catalogService,
        IVehicleStatusService statusService)
    {
        _vehicleService = vehicleService;
        _catalogService = catalogService;
        _statusService = statusService;
    }

    /// <summary>
    /// Danh sách xe với filter và phân trang
    /// </summary>
    public async Task<IActionResult> Index(VehicleFilterDto filter)
    {
        var result = await _vehicleService.GetAllVehiclesAsync(filter);
        
        // Prepare filter dropdowns
        await PrepareFilterDropdowns(filter);
        
        ViewBag.Filter = filter;
        return View(result);
    }

    /// <summary>
    /// Chi tiết xe
    /// </summary>
    public async Task<IActionResult> Details(Guid id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            TempData["Error"] = "Xe không tồn tại";
            return RedirectToAction(nameof(Index));
        }

        // Get allowed status transitions
        var allowedTransitions = await _statusService.GetAllowedTransitionsAsync(id);
        ViewBag.AllowedTransitions = allowedTransitions.Select(s => new SelectListItem
        {
            Value = ((int)s).ToString(),
            Text = VehicleStatusHelper.GetDisplayName(s)
        }).ToList();

        return View(vehicle);
    }

    /// <summary>
    /// Form tạo xe mới (GET)
    /// </summary>
    public async Task<IActionResult> Create()
    {
        await PrepareFormDropdowns();
        return View(new VehicleCreateDto());
    }

    /// <summary>
    /// Xử lý tạo xe mới (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PrepareFormDropdowns();
            return View(dto);
        }

        // TODO: Get current user ID from authentication
        var userId = Guid.Empty; // Placeholder

        var result = await _vehicleService.CreateVehicleAsync(dto, userId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await PrepareFormDropdowns();
        return View(dto);
    }

    /// <summary>
    /// Form sửa xe (GET)
    /// </summary>
    public async Task<IActionResult> Edit(Guid id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            TempData["Error"] = "Xe không tồn tại";
            return RedirectToAction(nameof(Index));
        }

        var dto = new VehicleUpdateDto
        {
            PlateNo = vehicle.PlateNo,
            ModelId = vehicle.ModelId,
            BranchId = vehicle.BranchId,
            Color = vehicle.Color,
            ManufactureYear = vehicle.ManufactureYear,
            CurrentOdoKm = vehicle.CurrentOdoKm
        };

        await PrepareFormDropdowns();
        ViewBag.VehicleId = id;
        return View(dto);
    }

    /// <summary>
    /// Xử lý sửa xe (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, VehicleUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PrepareFormDropdowns();
            ViewBag.VehicleId = id;
            return View(dto);
        }

        // TODO: Get current user ID from authentication
        var userId = Guid.Empty; // Placeholder

        var result = await _vehicleService.UpdateVehicleAsync(id, dto, userId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        await PrepareFormDropdowns();
        ViewBag.VehicleId = id;
        return View(dto);
    }

    /// <summary>
    /// Xóa xe (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _vehicleService.DeleteVehicleAsync(id);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Errors.FirstOrDefault() ?? "Có lỗi xảy ra";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Thay đổi trạng thái xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ChangeStatus([FromBody] VehicleStatusChangeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        // TODO: Get current user ID from authentication
        var userId = Guid.Empty; // Placeholder

        var result = await _statusService.ChangeStatusAsync(dto, userId);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault() 
        });
    }

    /// <summary>
    /// Lấy lịch sử trạng thái xe (Partial)
    /// </summary>
    public async Task<IActionResult> StatusHistory(Guid id, int limit = 20)
    {
        var history = await _statusService.GetStatusHistoryAsync(id, limit);
        return PartialView("_StatusHistory", history);
    }

    /// <summary>
    /// Lấy danh sách xe khả dụng (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Available(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var vehicles = await _vehicleService.GetAvailableVehiclesAsync(fromDate, toDate);
        return Json(vehicles);
    }

    /// <summary>
    /// Kiểm tra biển số tồn tại (AJAX validation)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckPlateNo(string plateNo, Guid? excludeId = null)
    {
        var exists = await _vehicleService.IsPlateNoExistsAsync(plateNo, excludeId);
        return Json(!exists); // Return true if NOT exists (valid)
    }

    #region Private Methods

    private async Task PrepareFilterDropdowns(VehicleFilterDto filter)
    {
        var vehicleTypes = await _catalogService.GetAllVehicleTypesAsync();
        var makes = await _vehicleService.GetMakesAsync();
        var branches = await _vehicleService.GetBranchesAsync();

        ViewBag.VehicleTypes = new SelectList(
            vehicleTypes.Select(vt => new { vt.VehicleTypeId, vt.TypeName }),
            "VehicleTypeId", "TypeName", filter.VehicleTypeId);

        ViewBag.Makes = new SelectList(
            makes.Select(m => new { Value = m, Text = m }),
            "Value", "Text", filter.Make);

        ViewBag.Branches = new SelectList(
            branches.Select(b => new { Id = b.Id, Name = b.Name }),
            "Id", "Name", filter.BranchId);

        ViewBag.Statuses = Enum.GetValues<VehicleStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = VehicleStatusHelper.GetDisplayName(s),
                Selected = filter.Status == s
            }).ToList();
    }

    private async Task PrepareFormDropdowns()
    {
        var vehicleModels = await _catalogService.GetAllVehicleModelsAsync();
        var branches = await _vehicleService.GetBranchesAsync();

        ViewBag.VehicleModels = new SelectList(
            vehicleModels.Select(vm => new { 
                vm.ModelId, 
                DisplayName = $"{vm.Make} {vm.ModelName} ({vm.VehicleTypeName})" 
            }),
            "ModelId", "DisplayName");

        ViewBag.Branches = new SelectList(
            branches.Select(b => new { Id = b.Id, Name = b.Name }),
            "Id", "Name");
    }

    #endregion
}
