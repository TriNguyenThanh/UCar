using UCar.Models;
using UCar.Models.DTOs;

namespace UCar.Interfaces;

public interface IHolidayService
{
    /// <summary>
    /// Get all holidays
    /// </summary>
    Task<List<HolidayConfig>> GetAllHolidaysAsync();
    
    /// <summary>
    /// Get holidays for specific year
    /// </summary>
    Task<List<HolidayConfig>> GetHolidaysByYearAsync(int year);
    
    /// <summary>
    /// Get active holidays only
    /// </summary>
    Task<List<HolidayConfig>> GetActiveHolidaysAsync();
    
    /// <summary>
    /// Get holiday by ID
    /// </summary>
    Task<HolidayConfig?> GetHolidayByIdAsync(Guid holidayId);
    
    /// <summary>
    /// Create new holiday
    /// </summary>
    Task<HolidayConfig> CreateHolidayAsync(HolidayDto dto, Guid createdBy);
    
    /// <summary>
    /// Update existing holiday
    /// </summary>
    Task<bool> UpdateHolidayAsync(Guid holidayId, HolidayDto dto);
    
    /// <summary>
    /// Delete holiday
    /// </summary>
    Task<bool> DeleteHolidayAsync(Guid holidayId);
    
    /// <summary>
    /// Toggle holiday active status
    /// </summary>
    Task<bool> ToggleHolidayStatusAsync(Guid holidayId);
    
    /// <summary>
    /// Check if date range overlaps with any active holiday
    /// </summary>
    Task<bool> HasHolidayOverlapAsync(DateTime startDate, DateTime endDate, Guid? excludeHolidayId = null);
    
    /// <summary>
    /// Count number of holiday days in date range
    /// </summary>
    Task<int> CountHolidayDaysAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get holidays in date range
    /// </summary>
    Task<List<HolidayConfig>> GetHolidaysInRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Check if specific date is a holiday
    /// </summary>
    Task<bool> IsHolidayAsync(DateTime date);
}
