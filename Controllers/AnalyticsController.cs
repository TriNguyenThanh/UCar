using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;

namespace UCar.Controllers;

/// <summary>
/// Controller cho báo cáo và phân tích (Module 9)
/// Tương ứng DFD 9.0: Báo cáo & phân tích
/// </summary>
[Authorize(Roles = "Admin,BranchManager")]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard tổng quan - Redirect to Revenue report
    /// </summary>
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Revenue));
    }

    /// <summary>
    /// Báo cáo doanh thu, lợi nhuận và chi phí (DFD 9.1)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            // Mặc định: 12 tháng gần nhất
            var from = fromDate ?? DateTime.Today.AddMonths(-12);
            var to = toDate ?? DateTime.Today;

            _logger.LogInformation("Revenue report requested: From {FromDate} to {ToDate}", from, to);

            var report = await _analyticsService.GetRevenueReportAsync(from, to);

            // Pass filter values to view
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            TempData["Error"] = "Có lỗi xảy ra khi tạo báo cáo doanh thu.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Báo cáo tỷ lệ sử dụng xe (DFD 9.2)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> VehicleUtilization()
    {
        try
        {
            _logger.LogInformation("Vehicle utilization report requested");

            var report = await _analyticsService.GetVehicleUtilizationReportAsync();

            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating vehicle utilization report");
            TempData["Error"] = "Có lỗi xảy ra khi tạo báo cáo tỷ lệ sử dụng xe.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Báo cáo khách hàng (DFD 9.3)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Customers(int months = 6)
    {
        try
        {
            _logger.LogInformation("Customer report requested: Last {Months} months", months);

            var report = await _analyticsService.GetCustomerReportAsync(months);

            ViewBag.Months = months;

            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer report");
            TempData["Error"] = "Có lỗi xảy ra khi tạo báo cáo khách hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
