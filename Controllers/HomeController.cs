using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels;

namespace UCar.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UCarDbContext _context;

    public HomeController(ILogger<HomeController> logger, UCarDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        // Customer redirect to vehicle search page
        if (User.IsInRole("Customer"))
        {
            return RedirectToAction("Search", "Booking");
        }

        var viewModel = new DashboardViewModel();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Stats for Admin/Staff
        viewModel.TotalCustomers = await _context.Customers.CountAsync();
        viewModel.TotalActiveVehicles = await _context.Vehicles
            .CountAsync(v => v.CurrentStatus != VehicleStatus.Decommissioned);
        viewModel.TodayBookings = await _context.Bookings
            .CountAsync(b => b.CreatedAt.Date == today);

        // Vehicle status counts
        viewModel.RentingVehicles = await _context.Vehicles
            .CountAsync(v => v.CurrentStatus == VehicleStatus.Renting);
        viewModel.MaintenanceVehicles = await _context.Vehicles
            .CountAsync(v => v.CurrentStatus == VehicleStatus.Maintenance);
        viewModel.AvailableVehicles = await _context.Vehicles
            .CountAsync(v => v.CurrentStatus == VehicleStatus.Available);

        // Monthly revenue (from completed contracts)
        viewModel.MonthlyRevenue = await _context.RentalContracts
            .Where(rc => rc.Status == RentalContractStatus.Completed && rc.ActualEnd >= monthStart)
            .SumAsync(rc => rc.TotalAmountFinal);

        // Popular vehicles (top 6 with most bookings)
        viewModel.PopularVehicles = await _context.Vehicles
            .Include(v => v.Model)
                .ThenInclude(m => m.VehicleType)
                    .ThenInclude(vt => vt.Prices)
            .OrderByDescending(v => v.Bookings.Count)
            .Take(6)
            .Select(v => new VehicleSummary
            {
                VehicleId = v.VehicleId,
                Make = v.Model.Make,
                ModelName = v.Model.ModelName,
                TypeName = v.Model.VehicleType.TypeName,
                Seats = v.Model.Seats,
                Transmission = v.Model.Transmission != null ? v.Model.Transmission.ToString() : "Tự động",
                Status = v.CurrentStatus,
                PlateNo = v.PlateNo,
                DailyPrice = v.Model.VehicleType.Prices
                    .Where(p => p.IsActive)
                    .Select(p => p.DailyBasePrice)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Recent bookings (last 10)
        viewModel.RecentBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.AssignedVehicle)
                .ThenInclude(v => v!.Model)
            .Include(b => b.VehicleType)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .Select(b => new BookingSummary
            {
                BookingId = b.BookingId,
                BookingCode = $"#BK-{b.BookingId.ToString().Substring(0, 6).ToUpper()}",
                CustomerName = b.Customer.FullName,
                CustomerInitials = GetInitials(b.Customer.FullName),
                VehicleName = b.AssignedVehicle != null 
                    ? $"{b.AssignedVehicle.Model.Make} {b.AssignedVehicle.Model.ModelName}" 
                    : b.VehicleType.TypeName,
                StartAt = b.StartAt,
                EndAt = b.EndAt,
                Status = b.Status,
                EstimatedTotal = b.EstimatedTotal
            })
            .ToListAsync();

        // New bookings count (pending)
        viewModel.NewBookingsCount = await _context.Bookings
            .CountAsync(b => b.Status == BookingStatus.Pending);

        // Monthly statistics
        viewModel.MonthlyBookingsCount = await _context.Bookings
            .CountAsync(b => b.CreatedAt >= monthStart);

        var completedCount = await _context.Bookings
            .CountAsync(b => b.CreatedAt >= monthStart && b.Status == BookingStatus.Completed);
        var totalMonthlyBookings = viewModel.MonthlyBookingsCount > 0 ? viewModel.MonthlyBookingsCount : 1;
        viewModel.CompletionRate = Math.Round((decimal)completedCount / totalMonthlyBookings * 100, 1);

        // Average rating (placeholder - would need a review/rating system)
        viewModel.AverageRating = 4.8m;

        return View(viewModel);
    }

    private static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "??";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }

    [AllowAnonymous]
    public IActionResult Demo()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
