using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;

namespace UCar.Services;

public class HolidayService : IHolidayService
{
    private readonly UCarDbContext _context;

    public HolidayService(UCarDbContext context)
    {
        _context = context;
    }

    public async Task<List<HolidayConfig>> GetAllHolidaysAsync()
    {
        return await _context.HolidayConfigs
            .OrderBy(h => h.Year)
            .ThenBy(h => h.StartDate)
            .ToListAsync();
    }

    public async Task<List<HolidayConfig>> GetHolidaysByYearAsync(int year)
    {
        return await _context.HolidayConfigs
            .Where(h => h.Year == year)
            .OrderBy(h => h.StartDate)
            .ToListAsync();
    }

    public async Task<List<HolidayConfig>> GetActiveHolidaysAsync()
    {
        return await _context.HolidayConfigs
            .Where(h => h.IsActive)
            .OrderBy(h => h.Year)
            .ThenBy(h => h.StartDate)
            .ToListAsync();
    }

    public async Task<HolidayConfig?> GetHolidayByIdAsync(Guid holidayId)
    {
        return await _context.HolidayConfigs
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId);
    }

    public async Task<HolidayConfig> CreateHolidayAsync(HolidayDto dto, Guid createdBy)
    {
        var holiday = new HolidayConfig
        {
            HolidayId = Guid.NewGuid(),
            HolidayName = dto.HolidayName,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Year = dto.Year,
            IsActive = dto.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        _context.HolidayConfigs.Add(holiday);
        await _context.SaveChangesAsync();

        return holiday;
    }

    public async Task<bool> UpdateHolidayAsync(Guid holidayId, HolidayDto dto)
    {
        var holiday = await _context.HolidayConfigs
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId);

        if (holiday == null)
            return false;

        holiday.HolidayName = dto.HolidayName;
        holiday.StartDate = dto.StartDate.Date;
        holiday.EndDate = dto.EndDate.Date;
        holiday.Year = dto.Year;
        holiday.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteHolidayAsync(Guid holidayId)
    {
        var holiday = await _context.HolidayConfigs
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId);

        if (holiday == null)
            return false;

        _context.HolidayConfigs.Remove(holiday);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleHolidayStatusAsync(Guid holidayId)
    {
        var holiday = await _context.HolidayConfigs
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId);

        if (holiday == null)
            return false;

        holiday.IsActive = !holiday.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasHolidayOverlapAsync(DateTime startDate, DateTime endDate, Guid? excludeHolidayId = null)
    {
        var query = _context.HolidayConfigs
            .Where(h => h.IsActive &&
                       h.StartDate <= endDate.Date &&
                       h.EndDate >= startDate.Date);

        if (excludeHolidayId.HasValue)
        {
            query = query.Where(h => h.HolidayId != excludeHolidayId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CountHolidayDaysAsync(DateTime startDate, DateTime endDate)
    {
        var holidays = await _context.HolidayConfigs
            .Where(h => h.IsActive)
            .Where(h => h.EndDate >= startDate.Date && h.StartDate <= endDate.Date)
            .ToListAsync();

        int count = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (holidays.Any(h => date >= h.StartDate.Date && date <= h.EndDate.Date))
            {
                count++;
            }
        }

        return count;
    }

    public async Task<List<HolidayConfig>> GetHolidaysInRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.HolidayConfigs
            .Where(h => h.IsActive)
            .Where(h => h.EndDate >= startDate.Date && h.StartDate <= endDate.Date)
            .OrderBy(h => h.StartDate)
            .ToListAsync();
    }

    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        return await _context.HolidayConfigs
            .Where(h => h.IsActive)
            .AnyAsync(h => date.Date >= h.StartDate.Date && date.Date <= h.EndDate.Date);
    }
}
