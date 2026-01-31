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
/// Controller quản lý bảng giá thuê xe
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
[Authorize(Roles = "Admin")]
public class PricingController : Controller
{
    private readonly IPricingService _pricingService;
    private readonly UCarDbContext _context;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IPricingService pricingService,
        UCarDbContext context,
        ILogger<PricingController> logger)
    {
        _pricingService = pricingService;
        _context = context;
        _logger = logger;
    }

    #region Index - Danh sách bảng giá

    /// <summary>
    /// Hiển thị danh sách bảng giá với filter và phân trang
    /// GET: /Pricing/Index
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(PricingSearchViewModel searchModel)
    {
        try
        {
            var result = await _pricingService.GetPricesAsync(
                searchModel.VehicleTypeId,
                searchModel.IsActive,
                searchModel.PageNumber,
                searchModel.PageSize);

            // Map to ListViewModel
            var viewModels = result.Items.Select(p => new PricingListViewModel
            {
                PriceId = p.PriceId,
                Name = p.Name,
                VehicleTypeName = p.VehicleType.TypeName,
                VehicleTypeId = p.VehicleTypeId,
                DailyBasePrice = p.DailyBasePrice,
                MonthlyMultiplier = p.MonthlyMultiplier,
                HolidayMultiplier = p.HolidayMultiplier,
                WeekendMultiplier = p.WeekendMultiplier,
                OvertimeHourlyPrice = p.OvertimeHourlyPrice,
                DepositSuggest = p.DepositSuggest,
                IsActive = p.IsActive,
                ContractCount = p.RentalContracts.Count
            }).ToList();

            var pagedList = new PaginatedList<PricingListViewModel>(
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
            _logger.LogError(ex, "Error loading pricing list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách bảng giá";
            return View(new PaginatedList<PricingListViewModel>(
                new List<PricingListViewModel>(), 0, 1, 20));
        }
    }

    #endregion

    #region Create - Tạo bảng giá mới

    /// <summary>
    /// Form tạo bảng giá mới
    /// GET: /Pricing/Create
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareFormDropdowns();
        return View(new PricingCreateViewModel());
    }

    /// <summary>
    /// Xử lý tạo bảng giá mới
    /// POST: /Pricing/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PricingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PrepareFormDropdowns();
            return View(model);
        }

        try
        {
            var price = new Price
            {
                VehicleTypeId = model.VehicleTypeId,
                Name = model.Name,
                DailyBasePrice = model.DailyBasePrice,
                MonthlyMultiplier = model.MonthlyMultiplier,
                HolidayMultiplier = model.HolidayMultiplier,
                WeekendMultiplier = model.WeekendMultiplier,
                OvertimeHourlyPrice = model.OvertimeHourlyPrice,
                DepositSuggest = model.DepositSuggest,
                IsActive = model.IsActive
            };

            var result = await _pricingService.CreatePriceAsync(price);

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
            _logger.LogError(ex, "Error creating price");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo bảng giá";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Edit - Chỉnh sửa bảng giá

    /// <summary>
    /// Form chỉnh sửa bảng giá
    /// GET: /Pricing/Edit/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var price = await _pricingService.GetPriceByIdAsync(id);
            if (price == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bảng giá";
                return RedirectToAction(nameof(Index));
            }

            var model = new PricingEditViewModel
            {
                PriceId = price.PriceId,
                VehicleTypeId = price.VehicleTypeId,
                Name = price.Name,
                DailyBasePrice = price.DailyBasePrice,
                MonthlyMultiplier = price.MonthlyMultiplier,
                HolidayMultiplier = price.HolidayMultiplier,
                WeekendMultiplier = price.WeekendMultiplier,
                OvertimeHourlyPrice = price.OvertimeHourlyPrice,
                DepositSuggest = price.DepositSuggest,
                IsActive = price.IsActive,
                ContractCount = price.RentalContracts.Count,
                IsUsedInContracts = price.RentalContracts.Any()
            };

            await PrepareFormDropdowns();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading price for edit: {PriceId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin bảng giá";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Xử lý chỉnh sửa bảng giá
    /// POST: /Pricing/Edit/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PricingEditViewModel model)
    {
        if (id != model.PriceId)
        {
            Console.WriteLine(model.PriceId.ToString(), id.ToString() + "LOGGING MISMATCH");
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
            var price = new Price
            {
                PriceId = model.PriceId,
                VehicleTypeId = model.VehicleTypeId,
                Name = model.Name,
                DailyBasePrice = model.DailyBasePrice,
                MonthlyMultiplier = model.MonthlyMultiplier,
                HolidayMultiplier = model.HolidayMultiplier,
                WeekendMultiplier = model.WeekendMultiplier,
                OvertimeHourlyPrice = model.OvertimeHourlyPrice,
                DepositSuggest = model.DepositSuggest,
                IsActive = model.IsActive
            };

            var result = await _pricingService.UpdatePriceAsync(id, price);

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
            _logger.LogError(ex, "Error updating price: {PriceId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật bảng giá";
            await PrepareFormDropdowns();
            return View(model);
        }
    }

    #endregion

    #region Details - Chi tiết bảng giá

    /// <summary>
    /// Hiển thị chi tiết bảng giá
    /// GET: /Pricing/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var price = await _pricingService.GetPriceByIdAsync(id);
            if (price == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bảng giá";
                return RedirectToAction(nameof(Index));
            }

            var model = new PricingDetailsViewModel
            {
                PriceId = price.PriceId,
                Name = price.Name,
                VehicleTypeName = price.VehicleType.TypeName,
                VehicleTypeId = price.VehicleTypeId,
                DailyBasePrice = price.DailyBasePrice,
                MonthlyMultiplier = price.MonthlyMultiplier,
                HolidayMultiplier = price.HolidayMultiplier,
                WeekendMultiplier = price.WeekendMultiplier,
                MonthlyPrice = price.MonthlyPrice,
                HolidayDailyPrice = price.HolidayDailyPrice,
                WeekendDailyPrice = price.WeekendDailyPrice,
                OvertimeHourlyPrice = price.OvertimeHourlyPrice,
                DepositSuggest = price.DepositSuggest,
                IsActive = price.IsActive,
                ContractCount = price.RentalContracts.Count,
                TotalRevenue = price.RentalContracts.Sum(c => c.RentalAmount),
                RecentContracts = price.RentalContracts
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .Select(c => new PriceContractUsageViewModel
                    {
                        ContractId = c.ContractId,
                        ContractCode = c.ContractCode,
                        CustomerName = c.Customer.FullName,
                        VehiclePlateNo = c.Vehicle.PlateNo,
                        StartDate = c.PlannedStart,
                        EndDate = c.PlannedEnd,
                        TotalAmount = c.TotalAmountFinal
                    }).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading price details: {PriceId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết bảng giá";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region Delete - Xóa bảng giá

    /// <summary>
    /// Xóa bảng giá
    /// POST: /Pricing/Delete/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _pricingService.DeletePriceAsync(id);
            foreach (String each in result.Errors){
                Console.WriteLine("Error: " + each);    
            }
            Console.WriteLine("Delete attempt for PriceId: " + id.ToString() + result.Success.ToString() + " " + result.Errors.ToString());

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
            _logger.LogError(ex, "Error deleting price: {PriceId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa bảng giá";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region ToggleActive - Bật/tắt trạng thái

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của bảng giá
    /// POST: /Pricing/ToggleActive/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive)
    {
        try
        {
            var result = await _pricingService.SetPriceActiveStatusAsync(id, isActive);

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
            _logger.LogError(ex, "Error toggling price active status: {PriceId}", id);
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
