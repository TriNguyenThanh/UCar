using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models.Enums;
using UCar.ViewModels.Booking;

namespace UCar.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out Guid id)) return id;
        return Guid.Empty;
    }

    // GET: /Booking
    // Admin/Staff sees all, Customer sees theirs (redirect)
    public IActionResult Index()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Staff"))
        {
            return RedirectToAction(nameof(Manage));
        }
        return RedirectToAction(nameof(MyBookings));
    }

    // GET: /Booking/Search
    [AllowAnonymous]
    public async Task<IActionResult> Search(DateTime? startDate, string? startTime, DateTime? endDate, string? endTime)
    {
        var vm = new BookingSearchCheckVM();
        
        if (startDate.HasValue && endDate.HasValue)
        {
            // Parse time strings (format: "HH:mm" or "H:mm")
            TimeSpan parsedStartTime = TimeSpan.Zero;
            TimeSpan parsedEndTime = TimeSpan.Zero;
            
            if (!string.IsNullOrEmpty(startTime))
            {
                TimeSpan.TryParse(startTime, out parsedStartTime);
            }
            if (!string.IsNullOrEmpty(endTime))
            {
                TimeSpan.TryParse(endTime, out parsedEndTime);
            }
            
            vm.StartDate = startDate;
            vm.StartTime = parsedStartTime;
            vm.EndDate = endDate;
            vm.EndTime = parsedEndTime;
            
            // Normalize DateTimes
            var start = startDate.Value.Date + parsedStartTime;
            var end = endDate.Value.Date + parsedEndTime;

            ViewBag.Results = await _bookingService.SearchVehiclesAsync(start, end);
            ViewBag.SearchPerformed = true;
        }
        else
        {
            // Default: Tomorrow 8AM to DayAfterTomorrow 8PM
            vm.StartDate = DateTime.Today.AddDays(1);
            vm.StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)).ToTimeSpan();
            vm.EndDate = DateTime.Today.AddDays(2);
            vm.EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(20)).ToTimeSpan();
        }

        return View(vm);
    }

    // GET: /Booking/Create?vehicleId=...&start=...&end=...
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(Guid vehicleId, DateTime start, DateTime end)
    {
        var vehicleInfo = await _bookingService.GetVehicleForBookingAsync(vehicleId, start, end);
        if (vehicleInfo == null) return NotFound("Xe không tồn tại hoặc không khả dụng.");

        var vm = new BookingCreateVM
        {
            VehicleId = vehicleId,
            ModelName = vehicleInfo.ModelName,
            PlateNo = vehicleInfo.PlateNo,
            ImageUrl = vehicleInfo.ImageUrl,
            StartAt = start,
            EndAt = end,
            EstimatedPrice = vehicleInfo.EstimatedTotal,
            TotalAmount = vehicleInfo.EstimatedTotal,
            
            // Pre-fill user info (mock or fetch from UserService if available, here just pass from User claims if possible)
            CustomerName = User.Identity?.Name ?? ""
        };

        return View(vm);
    }

    // POST: /Booking/Create
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateVM model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var bookingId = await _bookingService.CreateBookingAsync(GetCurrentUserId(), model);
            TempData["SuccessMessage"] = "To yêu cầu đặt xe thành công!";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // GET: /Booking/MyBookings
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyBookings()
    {
        var list = await _bookingService.GetMyBookingsAsync(GetCurrentUserId());
        return View(list);
    }

    // GET: /Booking/Manage
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Manage(BookingStatus? status)
    {
        var list = await _bookingService.GetAllBookingsAsync(status);
        return View(list);
    }

    // GET: /Booking/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var vm = await _bookingService.GetBookingDetailAsync(id, GetCurrentUserId(), isAdmin);
        
        if (vm == null) return NotFound();
        return View(vm);
    }

    // POST: /Booking/Confirm
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid bookingId)
    {
        try
        {
            await _bookingService.ConfirmBookingAsync(bookingId, GetCurrentUserId());
            TempData["SuccessMessage"] = "Đã xác nhận đơn đặt xe.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = bookingId });
    }

    // POST: /Booking/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(BookingActionVM model)
    {
        try
        {
            await _bookingService.CancelBookingAsync(model.BookingId, GetCurrentUserId(), model.Reason ?? "Cancelled by User");
            TempData["SuccessMessage"] = "Đã hủy đơn đặt xe.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = model.BookingId });
    }
    
    // POST: /Booking/Reject
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(BookingActionVM model)
    {
        try
        {
            await _bookingService.RejectBookingAsync(model.BookingId, GetCurrentUserId(), model.Reason ?? "Rejected by Staff");
            TempData["SuccessMessage"] = "Đã từ chối đơn đặt xe.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = model.BookingId });
    }
}
