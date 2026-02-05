using UCar.Models.DTOs;

namespace UCar.Interfaces;

public interface IPriceCalculationService
{
    /// <summary>
    /// Calculate price estimate for a rental period
    /// </summary>
    /// <param name="vehicleModelId">ID of vehicle model to rent</param>
    /// <param name="pickupDate">Rental start date</param>
    /// <param name="returnDate">Rental end date</param>
    /// <returns>Price estimate with breakdown details</returns>
    Task<PriceEstimateDto?> CalculateEstimateAsync(Guid vehicleModelId, DateTime pickupDate, DateTime returnDate);
    
    /// <summary>
    /// Count number of holidays in date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Number of holiday days in range</returns>
    Task<int> CountHolidaysInRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Generate detailed price breakdown items for contract
    /// </summary>
    /// <param name="vehicleModelId">Vehicle model ID</param>
    /// <param name="pickupDate">Rental start date</param>
    /// <param name="returnDate">Rental end date</param>
    /// <param name="baseDailyPrice">Snapshot base daily price</param>
    /// <param name="peakMultiplier">Snapshot peak multiplier</param>
    /// <param name="monthMultiplier">Snapshot monthly multiplier</param>
    /// <returns>List of breakdown line items</returns>
    Task<List<PriceBreakdownDto>> GenerateContractPriceDetailsAsync(
        Guid vehicleModelId, 
        DateTime pickupDate, 
        DateTime returnDate,
        decimal baseDailyPrice,
        decimal peakMultiplier,
        decimal monthMultiplier);
    
    /// <summary>
    /// Check if a specific date is a holiday
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>True if date is holiday</returns>
    Task<bool> IsHolidayAsync(DateTime date);
}
