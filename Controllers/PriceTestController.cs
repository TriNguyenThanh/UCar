using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;

namespace UCar.Controllers;

/// <summary>
/// Test controller for Phase 2-5 - Price Calculation, Holiday services, and Booking API
/// </summary>
public class PriceTestController : Controller
{
    private readonly IPriceCalculationService _priceCalc;
    private readonly IHolidayService _holidayService;
    private readonly IPricingService _pricingService;
    private readonly UCarDbContext _context;
    private readonly ILogger<PriceTestController> _logger;

    public PriceTestController(
        IPriceCalculationService priceCalc,
        IHolidayService holidayService,
        IPricingService pricingService,
        UCarDbContext context,
        ILogger<PriceTestController> logger)
    {
        _priceCalc = priceCalc;
        _holidayService = holidayService;
        _pricingService = pricingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: /PriceTest
    /// Test page with various scenarios
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var holidays = await _holidayService.GetActiveHolidaysAsync();
        ViewBag.Holidays = holidays;
        
        // Load available vehicle models for testing
        var models = await _context.VehicleModels
            .Include(vm => vm.VehicleType)
            .OrderBy(vm => vm.Make)
            .ThenBy(vm => vm.ModelName)
            .ToListAsync();
        ViewBag.VehicleModels = models;
        
        return View();
    }

    /// <summary>
    /// POST: /PriceTest/Calculate
    /// Calculate price estimate for given parameters
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Calculate(Guid vehicleModelId, DateTime pickupDate, DateTime returnDate)
    {
        if (returnDate <= pickupDate)
        {
            return Json(new { error = "Return date must be after pickup date" });
        }

        var estimate = await _priceCalc.CalculateEstimateAsync(vehicleModelId, pickupDate, returnDate);
        
        if (estimate == null)
        {
            return Json(new { error = "No pricing found for this vehicle model" });
        }

        return Json(new
        {
            success = true,
            vehicleModelName = estimate.VehicleModelName,
            pickupDate = estimate.PickupDate.ToString("dd/MM/yyyy"),
            returnDate = estimate.ReturnDate.ToString("dd/MM/yyyy"),
            totalDays = estimate.TotalDays,
            normalDays = estimate.NormalDays,
            peakDays = estimate.PeakDays,
            isMonthlyRate = estimate.IsMonthlyRate,
            baseDailyPrice = estimate.BaseDailyPrice,
            monthMultiplier = estimate.MonthMultiplier,
            peakMultiplier = estimate.PeakMultiplier,
            normalDaysAmount = estimate.NormalDaysAmount,
            peakDaysAmount = estimate.PeakDaysAmount,
            monthlyAmount = estimate.MonthlyAmount,
            subTotal = estimate.SubTotal,
            responsibilityDeposit = estimate.ResponsibilityDeposit,
            rentalDeposit = estimate.RentalDeposit,
            totalDeposit = estimate.TotalDeposit,
            totalAmount = estimate.TotalAmount,
            breakdown = estimate.Breakdown.Select(b => new
            {
                order = b.DisplayOrder,
                description = b.LineDescription,
                quantity = b.Quantity,
                unitPrice = b.UnitPrice,
                lineTotal = b.LineTotal
            })
        });
    }

    /// <summary>
    /// GET: /PriceTest/CountHolidays?start=2026-02-01&end=2026-02-10
    /// Test holiday counting
    /// </summary>
    public async Task<IActionResult> CountHolidays(DateTime start, DateTime end)
    {
        var count = await _priceCalc.CountHolidaysInRangeAsync(start, end);
        var holidays = await _holidayService.GetActiveHolidaysAsync();
        
        var overlappingHolidays = holidays.Where(h => 
            h.StartDate <= end.Date && h.EndDate >= start.Date
        ).ToList();

        return Json(new
        {
            startDate = start.ToString("dd/MM/yyyy"),
            endDate = end.ToString("dd/MM/yyyy"),
            totalDays = (end.Date - start.Date).Days,
            holidayDays = count,
            normalDays = (end.Date - start.Date).Days - count,
            overlappingHolidays = overlappingHolidays.Select(h => new
            {
                name = h.HolidayName,
                start = h.StartDate.ToString("dd/MM/yyyy"),
                end = h.EndDate.ToString("dd/MM/yyyy")
            })
        });
    }

    /// <summary>
    /// GET: /PriceTest/TestPhase5
    /// Test Phase 5 - PricingService and Booking API integration
    /// </summary>
    public async Task<IActionResult> TestPhase5()
    {
        var testResults = new List<object>();

        try
        {
            // Test 1: Get all active prices
            _logger.LogInformation("Testing PricingService.GetPricesAsync...");
            var pricesResult = await _pricingService.GetPricesAsync(
                vehicleModelId: null,
                isActive: true,
                page: 1,
                pageSize: 10
            );
            testResults.Add(new
            {
                test = "GetPricesAsync (active only)",
                success = true,
                totalCount = pricesResult.TotalCount,
                priceCount = pricesResult.Items.Count()
            });

            // Test 2: Get price for specific vehicle model
            var firstModel = await _context.VehicleModels.FirstOrDefaultAsync();
            if (firstModel != null)
            {
                _logger.LogInformation($"Testing GetActivePriceByVehicleModelAsync for {firstModel.ModelName}...");
                var price = await _pricingService.GetActivePriceByVehicleModelAsync(firstModel.ModelId);
                testResults.Add(new
                {
                    test = $"GetActivePriceByVehicleModelAsync for {firstModel.ModelName}",
                    success = price != null,
                    hasPrice = price != null,
                    dailyPrice = price?.BaseDailyPrice.ToString("N0") + " VND"
                });

                // Test 3: Calculate estimate (same as Booking API)
                var startDate = DateTime.Today.AddDays(1);
                var endDate = startDate.AddDays(5);
                _logger.LogInformation($"Testing CalculateEstimateAsync from {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}...");
                var estimate = await _priceCalc.CalculateEstimateAsync(firstModel.ModelId, startDate, endDate);
                testResults.Add(new
                {
                    test = $"CalculateEstimateAsync (5 days)",
                    success = estimate != null,
                    vehicleModel = estimate?.VehicleModelName,
                    totalDays = estimate?.TotalDays,
                    normalDays = estimate?.NormalDays,
                    peakDays = estimate?.PeakDays,
                    totalAmount = estimate?.TotalAmount.ToString("N0") + " VND",
                    totalDeposit = estimate?.TotalDeposit.ToString("N0") + " VND"
                });

                // Test 4: Monthly rate scenario (>= 30 days)
                var longEndDate = startDate.AddDays(35);
                _logger.LogInformation($"Testing CalculateEstimateAsync for monthly rate (35 days)...");
                var monthlyEstimate = await _priceCalc.CalculateEstimateAsync(firstModel.ModelId, startDate, longEndDate);
                testResults.Add(new
                {
                    test = $"CalculateEstimateAsync (35 days - monthly rate)",
                    success = monthlyEstimate != null,
                    isMonthlyRate = monthlyEstimate?.IsMonthlyRate,
                    totalDays = monthlyEstimate?.TotalDays,
                    monthlyAmount = monthlyEstimate?.MonthlyAmount.ToString("N0") + " VND",
                    totalAmount = monthlyEstimate?.TotalAmount.ToString("N0") + " VND"
                });
            }

            // Test 5: Holiday counting
            var tetStart = new DateTime(2026, 1, 28);
            var tetEnd = new DateTime(2026, 2, 3);
            _logger.LogInformation($"Testing CountHolidayDaysAsync for Tết period...");
            var holidayCount = await _holidayService.CountHolidayDaysAsync(tetStart, tetEnd);
            testResults.Add(new
            {
                test = "CountHolidayDaysAsync (Tết period)",
                success = true,
                dateRange = $"{tetStart:dd/MM/yyyy} - {tetEnd:dd/MM/yyyy}",
                totalDays = (tetEnd - tetStart).Days,
                holidayDays = holidayCount,
                normalDays = (tetEnd - tetStart).Days - holidayCount
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Phase 5 testing");
            testResults.Add(new
            {
                test = "Exception occurred",
                success = false,
                error = ex.Message
            });
        }

        return Json(new
        {
            phase = "Phase 5 - Controllers & API Testing",
            timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
            results = testResults
        });
    }

    /// <summary>
    /// GET: /PriceTest/TestScenarios
    /// Predefined test scenarios
    /// </summary>
    public async Task<IActionResult> TestScenarios()
    {
        var scenarios = new List<object>();

        // Get first vehicle model for testing
        var firstPrice = await _priceCalc.CalculateEstimateAsync(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), // Will fail, just structure
            DateTime.Now,
            DateTime.Now.AddDays(5)
        );

        return Json(new
        {
            message = "Use /PriceTest to manually test scenarios",
            instructions = new[]
            {
                "Scenario 1: 5 days, no holidays (normal pricing)",
                "Scenario 2: 5 days including Tết (with peak pricing)",
                "Scenario 3: 35 days (monthly rate applies)",
                "Scenario 4: Date range spanning multiple holidays"
            }
        });
    }
}
