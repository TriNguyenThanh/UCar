using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UCar.Interfaces;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý danh mục xe (loại xe, dòng xe)
/// </summary>
public class VehicleCatalogController : Controller
{
    private readonly IVehicleCatalogService _catalogService;

    public VehicleCatalogController(IVehicleCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    #region Vehicle Types

    /// <summary>
    /// Danh sách loại xe
    /// </summary>
    public async Task<IActionResult> VehicleTypes()
    {
        var types = await _catalogService.GetAllVehicleTypesAsync();
        return View(types);
    }

    /// <summary>
    /// Tạo loại xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVehicleType([FromBody] VehicleTypeCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _catalogService.CreateVehicleTypeAsync(dto);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault(),
            id = result.Data
        });
    }

    /// <summary>
    /// Lấy thông tin loại xe (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVehicleType(Guid id)
    {
        var type = await _catalogService.GetVehicleTypeByIdAsync(id);
        if (type == null)
        {
            return Json(new { success = false, message = "Loại xe không tồn tại" });
        }
        return Json(new { success = true, data = type });
    }

    /// <summary>
    /// Cập nhật loại xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateVehicleType(Guid id, [FromBody] VehicleTypeUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _catalogService.UpdateVehicleTypeAsync(id, dto);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault() 
        });
    }

    /// <summary>
    /// Xóa loại xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteVehicleType(Guid id)
    {
        var result = await _catalogService.DeleteVehicleTypeAsync(id);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault() 
        });
    }

    #endregion

    #region Vehicle Models

    /// <summary>
    /// Danh sách dòng xe
    /// </summary>
    public async Task<IActionResult> VehicleModels(Guid? vehicleTypeId = null)
    {
        var models = await _catalogService.GetAllVehicleModelsAsync(vehicleTypeId);
        
        // Prepare filter dropdown
        var types = await _catalogService.GetAllVehicleTypesAsync();
        ViewBag.VehicleTypes = new SelectList(
            types.Select(vt => new { vt.VehicleTypeId, vt.TypeName }),
            "VehicleTypeId", "TypeName", vehicleTypeId);
        ViewBag.SelectedVehicleTypeId = vehicleTypeId;

        // Prepare create form dropdown
        ViewBag.VehicleTypesForCreate = types.ToList();

        // Transmission types for dropdown
        ViewBag.TransmissionTypes = Enum.GetValues<TransmissionType>()
            .Select(t => new SelectListItem
            {
                Value = ((int)t).ToString(),
                Text = t == TransmissionType.Auto ? "Tự động" : "Số sàn"
            }).ToList();

        return View(models);
    }

    /// <summary>
    /// Tạo dòng xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVehicleModel([FromBody] VehicleModelCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _catalogService.CreateVehicleModelAsync(dto);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault(),
            id = result.Data
        });
    }

    /// <summary>
    /// Lấy thông tin dòng xe (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVehicleModel(Guid id)
    {
        var model = await _catalogService.GetVehicleModelByIdAsync(id);
        if (model == null)
        {
            return Json(new { success = false, message = "Dòng xe không tồn tại" });
        }
        return Json(new { success = true, data = model });
    }

    /// <summary>
    /// Cập nhật dòng xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateVehicleModel(Guid id, [FromBody] VehicleModelUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _catalogService.UpdateVehicleModelAsync(id, dto);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault() 
        });
    }

    /// <summary>
    /// Xóa dòng xe (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteVehicleModel(Guid id)
    {
        var result = await _catalogService.DeleteVehicleModelAsync(id);
        
        return Json(new { 
            success = result.Success, 
            message = result.Success ? result.Message : result.Errors.FirstOrDefault() 
        });
    }

    /// <summary>
    /// Lấy danh sách dòng xe theo loại xe (AJAX - cho dropdown cascade)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetModelsByType(Guid vehicleTypeId)
    {
        var models = await _catalogService.GetAllVehicleModelsAsync(vehicleTypeId);
        return Json(models.Select(m => new { 
            m.ModelId, 
            DisplayName = $"{m.Make} {m.ModelName}" 
        }));
    }

    #endregion
}
