using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models.DTOs;

namespace UCar.Services;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly UCarDbContext _context;
    private readonly IDepositPolicyService _depositPolicyService;
    private const int MONTHLY_THRESHOLD_DAYS = 30; // >= 30 days uses monthly rate

    public PriceCalculationService(
        UCarDbContext context,
        IDepositPolicyService depositPolicyService)
    {
        _context = context;
        _depositPolicyService = depositPolicyService;
    }

    public async Task<PriceEstimateDto?> CalculateEstimateAsync(Guid vehicleModelId, DateTime pickupDate, DateTime returnDate)
    {
        // Validate dates
        if (returnDate <= pickupDate)
            return null;

        // Get price configuration for this vehicle model
        var price = await _context.Prices
            .Include(p => p.VehicleModel)
            .FirstOrDefaultAsync(p => p.VehicleModelId == vehicleModelId);

        if (price == null)
            return null;

        // Calculate rental days: count calendar days crossed
        // Example: 8h on Feb 5 to 20h on Feb 6 = spans 2 calendar days (5th and 6th) = 2 days
        var totalDays = (returnDate.Date - pickupDate.Date).Days + 1;

        // Count holiday days in range
        var peakDays = await CountHolidaysInRangeAsync(pickupDate, returnDate);
        var normalDays = totalDays - peakDays;

        // Determine if monthly rate applies
        bool isMonthlyRate = totalDays >= MONTHLY_THRESHOLD_DAYS;

        decimal normalDaysAmount = 0;
        decimal peakDaysAmount = 0;
        decimal monthlyAmount = 0;
        decimal subTotal = 0;

        if (isMonthlyRate)
        {
            // Monthly rate: only monthly amount, no peak pricing
            monthlyAmount = totalDays * price.BaseDailyPrice * price.MonthMultiplier;
            subTotal = monthlyAmount;
        }
        else
        {
            // Daily rate: base price + peak multiplier
            normalDaysAmount = normalDays * price.BaseDailyPrice;
            peakDaysAmount = peakDays * price.BaseDailyPrice * price.PeakMultiplier;
            subTotal = normalDaysAmount + peakDaysAmount;
        }

        // Calculate deposits using DepositPolicyService
        var depositBreakdown = await _depositPolicyService
            .CalculateDepositBreakdownAsync(vehicleModelId, subTotal);

        var totalAmount = subTotal + depositBreakdown.TotalDeposit;

        // Generate breakdown
        var breakdown = await GenerateContractPriceDetailsAsync(
            vehicleModelId, 
            pickupDate, 
            returnDate, 
            price.BaseDailyPrice, 
            price.PeakMultiplier, 
            price.MonthMultiplier);

        return new PriceEstimateDto
        {
            VehicleModelId = vehicleModelId,
            VehicleModelName = $"{price.VehicleModel?.Make} {price.VehicleModel?.ModelName}",
            PickupDate = pickupDate,
            ReturnDate = returnDate,
            TotalDays = totalDays,
            NormalDays = normalDays,
            PeakDays = peakDays,
            BaseDailyPrice = price.BaseDailyPrice,
            MonthMultiplier = price.MonthMultiplier,
            PeakMultiplier = price.PeakMultiplier,
            IsMonthlyRate = isMonthlyRate,
            NormalDaysAmount = normalDaysAmount,
            PeakDaysAmount = peakDaysAmount,
            MonthlyAmount = monthlyAmount,
            SubTotal = subTotal,
            ResponsibilityDeposit = depositBreakdown.ResponsibilityDeposit,
            RentalDeposit = depositBreakdown.RentalDeposit,
            TotalDeposit = depositBreakdown.TotalDeposit,
            TotalAmount = totalAmount,
            Breakdown = breakdown
        };
    }

    public async Task<int> CountHolidaysInRangeAsync(DateTime startDate, DateTime endDate)
    {
        // Get all active holidays that overlap with the rental period
        var holidays = await _context.HolidayConfigs
            .Where(h => h.IsActive && 
                       h.StartDate <= endDate.Date && 
                       h.EndDate >= startDate.Date)
            .ToListAsync();

        int holidayDays = 0;

        // Count each day in range (inclusive of both start and end dates)
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            // Check if this date falls within any holiday
            if (holidays.Any(h => date >= h.StartDate && date <= h.EndDate))
            {
                holidayDays++;
            }
        }

        return holidayDays;
    }

    public async Task<List<PriceBreakdownDto>> GenerateContractPriceDetailsAsync(
        Guid vehicleModelId, 
        DateTime pickupDate, 
        DateTime returnDate, 
        decimal baseDailyPrice, 
        decimal peakMultiplier, 
        decimal monthMultiplier)
    {
        var breakdown = new List<PriceBreakdownDto>();
        int order = 1;

        var totalDays = (returnDate.Date - pickupDate.Date).Days + 1;
        var peakDays = await CountHolidaysInRangeAsync(pickupDate, returnDate);
        var normalDays = totalDays - peakDays;
        bool isMonthlyRate = totalDays >= MONTHLY_THRESHOLD_DAYS;

        if (isMonthlyRate)
        {
            // Monthly rate breakdown
            var monthlyAmount = totalDays * baseDailyPrice * monthMultiplier;
            breakdown.Add(new PriceBreakdownDto
            {
                DisplayOrder = order++,
                LineDescription = $"Giá thuê tháng ({totalDays} ngày x {baseDailyPrice:N0} x {monthMultiplier})",
                Quantity = totalDays,
                UnitPrice = baseDailyPrice * monthMultiplier,
                LineTotal = monthlyAmount
            });
        }
        else
        {
            // Daily rate breakdown
            if (normalDays > 0)
            {
                var normalAmount = normalDays * baseDailyPrice;
                breakdown.Add(new PriceBreakdownDto
                {
                    DisplayOrder = order++,
                    LineDescription = $"Ngày thường ({normalDays} ngày x {baseDailyPrice:N0})",
                    Quantity = normalDays,
                    UnitPrice = baseDailyPrice,
                    LineTotal = normalAmount
                });
            }

            if (peakDays > 0)
            {
                var peakUnitPrice = baseDailyPrice * peakMultiplier;
                var peakAmount = peakDays * peakUnitPrice;
                breakdown.Add(new PriceBreakdownDto
                {
                    DisplayOrder = order++,
                    LineDescription = $"Ngày lễ/cao điểm ({peakDays} ngày x {baseDailyPrice:N0} x {peakMultiplier})",
                    Quantity = peakDays,
                    UnitPrice = peakUnitPrice,
                    LineTotal = peakAmount
                });
            }
        }

        // Add deposits using DepositPolicyService
        var subTotal = breakdown.Sum(b => b.LineTotal);
        var depositBreakdown = await _depositPolicyService
            .CalculateDepositBreakdownAsync(vehicleModelId, subTotal);

        breakdown.Add(new PriceBreakdownDto
        {
            DisplayOrder = order++,
            LineDescription = "Tiền cọc trách nhiệm",
            Quantity = 1,
            UnitPrice = depositBreakdown.ResponsibilityDeposit,
            LineTotal = depositBreakdown.ResponsibilityDeposit
        });

        breakdown.Add(new PriceBreakdownDto
        {
            DisplayOrder = order++,
            LineDescription = $"Tiền cọc thuê xe",
            Quantity = 1,
            UnitPrice = depositBreakdown.RentalDeposit,
            LineTotal = depositBreakdown.RentalDeposit
        });

        return breakdown;
    }

    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        return await _context.HolidayConfigs
            .AnyAsync(h => h.IsActive && 
                          date.Date >= h.StartDate && 
                          date.Date <= h.EndDate);
    }
}
