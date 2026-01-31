using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Models;
using UCar.ViewModels.Holiday;

namespace UCar.Controllers;

public class HolidayController : Controller
{
    private readonly UCarDbContext _context;
    private readonly ILogger<HolidayController> _logger;

    public HolidayController(UCarDbContext context, ILogger<HolidayController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Holiday
    public async Task<IActionResult> Index()
    {
        try
        {
            var holidays = await _context.HolidayConfigs
                .OrderBy(h => h.IsActive ? 0 : 1)
                .ThenBy(h => h.Date ?? DateTime.MaxValue)
                .ToListAsync();

            var viewModels = holidays.Select(h => new HolidayListViewModel
            {
                HolidayId = h.HolidayId,
                Name = h.Name,
                Date = h.Date,
                LunarDate = h.LunarDate,
                DateRange = h.StartDate.HasValue && h.EndDate.HasValue 
                    ? $"{h.StartDate.Value:dd/MM/yyyy} - {h.EndDate.Value:dd/MM/yyyy}"
                    : null,
                IsRecurring = h.IsRecurring,
                IsActive = h.IsActive
            }).ToList();

            return View(viewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading holidays");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách ngày lễ";
            return View(new List<HolidayListViewModel>());
        }
    }

    // GET: Holiday/Create
    public IActionResult Create()
    {
        return View(new HolidayCreateViewModel());
    }

    // POST: Holiday/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate at least one date type is provided
            if (!model.Date.HasValue && string.IsNullOrWhiteSpace(model.LunarDate) && !model.StartDate.HasValue)
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất một loại ngày: Dương lịch, Âm lịch, hoặc Kỳ nghỉ");
                return View(model);
            }

            // Validate date range
            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
                return View(model);
            }

            var holiday = new HolidayConfig
            {
                HolidayId = Guid.NewGuid(),
                Name = model.Name,
                Date = model.Date,
                LunarDate = model.LunarDate,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsRecurring = model.IsRecurring,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.HolidayConfigs.Add(holiday);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm ngày lễ thành công";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm ngày lễ";
            return View(model);
        }
    }

    // GET: Holiday/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var holiday = await _context.HolidayConfigs.FindAsync(id);
            if (holiday == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ngày lễ";
                return RedirectToAction(nameof(Index));
            }

            var model = new HolidayEditViewModel
            {
                HolidayId = holiday.HolidayId,
                Name = holiday.Name,
                Date = holiday.Date,
                LunarDate = holiday.LunarDate,
                StartDate = holiday.StartDate,
                EndDate = holiday.EndDate,
                IsRecurring = holiday.IsRecurring,
                Description = holiday.Description,
                IsActive = holiday.IsActive
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading holiday for edit: {HolidayId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin ngày lễ";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Holiday/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HolidayEditViewModel model)
    {
        if (id != model.HolidayId)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate at least one date type is provided
            if (!model.Date.HasValue && string.IsNullOrWhiteSpace(model.LunarDate) && !model.StartDate.HasValue)
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất một loại ngày: Dương lịch, Âm lịch, hoặc Kỳ nghỉ");
                return View(model);
            }

            // Validate date range
            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
                return View(model);
            }

            var holiday = await _context.HolidayConfigs.FindAsync(id);
            if (holiday == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ngày lễ";
                return RedirectToAction(nameof(Index));
            }

            holiday.Name = model.Name;
            holiday.Date = model.Date;
            holiday.LunarDate = model.LunarDate;
            holiday.StartDate = model.StartDate;
            holiday.EndDate = model.EndDate;
            holiday.IsRecurring = model.IsRecurring;
            holiday.Description = model.Description;
            holiday.IsActive = model.IsActive;
            holiday.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật ngày lễ thành công";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday: {HolidayId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật ngày lễ";
            return View(model);
        }
    }

    // POST: Holiday/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var holiday = await _context.HolidayConfigs.FindAsync(id);
            if (holiday == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ngày lễ";
                return RedirectToAction(nameof(Index));
            }

            _context.HolidayConfigs.Remove(holiday);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa ngày lễ thành công";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday: {HolidayId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa ngày lễ";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Holiday/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        try
        {
            var holiday = await _context.HolidayConfigs.FindAsync(id);
            if (holiday == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ngày lễ" });
            }

            holiday.IsActive = !holiday.IsActive;
            holiday.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = holiday.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling holiday status: {HolidayId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra" });
        }
    }
}
