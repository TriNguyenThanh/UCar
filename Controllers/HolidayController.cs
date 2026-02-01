using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models.DTOs;
using System.Security.Claims;

namespace UCar.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class HolidayController : Controller
{
    private readonly IHolidayService _holidayService;

    public HolidayController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    // GET: Holiday
    public async Task<IActionResult> Index(int? year)
    {
        var currentYear = year ?? DateTime.Now.Year;
        var holidays = await _holidayService.GetHolidaysByYearAsync(currentYear);
        
        ViewBag.CurrentYear = currentYear;
        ViewBag.AvailableYears = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();
        
        return View(holidays);
    }

    // GET: Holiday/Create
    public IActionResult Create()
    {
        var dto = new HolidayDto
        {
            Year = DateTime.Now.Year,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today,
            IsActive = true
        };
        return View(dto);
    }

    // POST: Holiday/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayDto dto)
    {
        if (dto.EndDate < dto.StartDate)
        {
            ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
        }

        // Check for overlapping holidays
        var hasOverlap = await _holidayService.HasHolidayOverlapAsync(dto.StartDate, dto.EndDate);
        if (hasOverlap)
        {
            ModelState.AddModelError("", "Khoảng thời gian này đã trùng với ngày lễ khác");
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _holidayService.CreateHolidayAsync(dto, userId);

        TempData["SuccessMessage"] = "Thêm ngày lễ thành công!";
        return RedirectToAction(nameof(Index), new { year = dto.Year });
    }

    // GET: Holiday/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var holiday = await _holidayService.GetHolidayByIdAsync(id);
        if (holiday == null)
        {
            return NotFound();
        }

        var dto = new HolidayDto
        {
            HolidayName = holiday.HolidayName,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate,
            Year = holiday.Year,
            IsActive = holiday.IsActive
        };

        return View(dto);
    }

    // POST: Holiday/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HolidayDto dto)
    {
        if (dto.EndDate < dto.StartDate)
        {
            ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
        }

        // Check for overlapping holidays (exclude current holiday)
        var hasOverlap = await _holidayService.HasHolidayOverlapAsync(dto.StartDate, dto.EndDate, id);
        if (hasOverlap)
        {
            ModelState.AddModelError("", "Khoảng thời gian này đã trùng với ngày lễ khác");
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var success = await _holidayService.UpdateHolidayAsync(id, dto);
        if (!success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Cập nhật ngày lễ thành công!";
        return RedirectToAction(nameof(Index), new { year = dto.Year });
    }

    // POST: Holiday/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _holidayService.DeleteHolidayAsync(id);
        if (!success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Xóa ngày lễ thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Holiday/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var success = await _holidayService.ToggleHolidayStatusAsync(id);
        if (!success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
        return RedirectToAction(nameof(Index));
    }
}
