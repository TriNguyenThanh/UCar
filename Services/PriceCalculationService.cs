using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models.DTOs;

namespace UCar.Services;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly UCarDbContext _context;
    private const decimal RESPONSIBILITY_DEPOSIT = 2_000_000m; // Fixed 2M VND
    private const decimal RENTAL_DEPOSIT_RATE = 0.5m; // 50% of rental cost
    private const int MONTHLY_THRESHOLD_DAYS = 30; // >= 30 days uses monthly rate

    public PriceCalculationService(UCarDbContext context)
    {
        _context = context;
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

        // Calculate total days
        var totalDays = (returnDate.Date - pickupDate.Date).Days;

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

        // Calculate deposits
        var rentalDeposit = subTotal * RENTAL_DEPOSIT_RATE;
        var totalDeposit = RESPONSIBILITY_DEPOSIT + rentalDeposit;
        var totalAmount = subTotal + totalDeposit;

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
            ResponsibilityDeposit = RESPONSIBILITY_DEPOSIT,
            RentalDeposit = rentalDeposit,
            TotalDeposit = totalDeposit,
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

        // Count each day in range
        for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
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

        var totalDays = (returnDate.Date - pickupDate.Date).Days;
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

        // Add deposits
        var subTotal = breakdown.Sum(b => b.LineTotal);
        var rentalDeposit = subTotal * RENTAL_DEPOSIT_RATE;

        breakdown.Add(new PriceBreakdownDto
        {
            DisplayOrder = order++,
            LineDescription = "Tiền cọc trách nhiệm",
            Quantity = 1,
            UnitPrice = RESPONSIBILITY_DEPOSIT,
            LineTotal = RESPONSIBILITY_DEPOSIT
        });

        breakdown.Add(new PriceBreakdownDto
        {
            DisplayOrder = order++,
            LineDescription = $"Tiền cọc thuê xe (50% giá thuê)",
            Quantity = 1,
            UnitPrice = rentalDeposit,
            LineTotal = rentalDeposit
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
