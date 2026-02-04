using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;

namespace UCar.Services;

/// <summary>
/// Service quản lý bảng giá thuê xe
/// </summary>
public class PricingService : IPricingService
{
    private readonly UCarDbContext _context;

    public PricingService(UCarDbContext context)
    {
        _context = context;
    }

    // ===== PRICE CRUD =====

    public async Task<PagedResult<Price>> GetPricesAsync(
        Guid? vehicleModelId = null, 
        bool? isActive = null, 
        int page = 1, 
        int pageSize = 20)
    {
        var query = _context.Prices
            .Include(p => p.VehicleModel)
            .ThenInclude(vm => vm.VehicleType)
            .AsQueryable();

        // Filter by vehicle model
        if (vehicleModelId.HasValue)
        {
            query = query.Where(p => p.VehicleModelId == vehicleModelId.Value);
        }

        // Filter by active status
        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(p => p.ValidFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Price>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Price?> GetPriceByIdAsync(Guid priceId)
    {
        return await _context.Prices
            .Include(p => p.VehicleModel)
            .ThenInclude(vm => vm.VehicleType)
            .FirstOrDefaultAsync(p => p.PriceId == priceId);
    }

    public async Task<Price?> GetActivePriceByVehicleModelAsync(Guid vehicleModelId)
    {
        var now = DateTime.Now;
        return await _context.Prices
            .Include(p => p.VehicleModel)
            .ThenInclude(vm => vm.VehicleType)
            .Where(p => p.VehicleModelId == vehicleModelId)
            .Where(p => p.IsActive)
            .Where(p => p.ValidFrom <= now)
            .Where(p => p.ValidTo == null || p.ValidTo >= now)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreatePriceAsync(Price price)
    {
        try
        {
            // Validate vehicle model exists
            var modelExists = await _context.VehicleModels
                .AnyAsync(vm => vm.ModelId == price.VehicleModelId);
            
            if (!modelExists)
            {
                return ServiceResult<Guid>.Fail("Model xe không tồn tại");
            }

            // Validate price values
            if (price.BaseDailyPrice <= 0)
            {
                return ServiceResult<Guid>.Fail("Giá cơ bản phải lớn hơn 0");
            }

            if (price.MonthMultiplier <= 0 || price.MonthMultiplier > 1)
            {
                return ServiceResult<Guid>.Fail("Hệ số tháng phải trong khoảng (0, 1]");
            }

            if (price.PeakMultiplier < 1)
            {
                return ServiceResult<Guid>.Fail("Hệ số lễ phải >= 1");
            }

            // Set defaults
            price.PriceId = Guid.NewGuid();

            _context.Prices.Add(price);
            await _context.SaveChangesAsync();

            return ServiceResult<Guid>.Ok(price.PriceId, "Tạo bảng giá thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult<Guid>.Fail($"Lỗi khi tạo bảng giá: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdatePriceAsync(Guid priceId, Price price)
    {
        try
        {
            var existing = await _context.Prices.FindAsync(priceId);
            if (existing == null)
            {
                return ServiceResult.Fail("Bảng giá không tồn tại");
            }

            // Validate price values
            if (price.BaseDailyPrice <= 0)
            {
                return ServiceResult.Fail("Giá cơ bản phải lớn hơn 0");
            }

            if (price.MonthMultiplier <= 0 || price.MonthMultiplier > 1)
            {
                return ServiceResult.Fail("Hệ số tháng phải trong khoảng (0, 1]");
            }

            if (price.PeakMultiplier < 1)
            {
                return ServiceResult.Fail("Hệ số lễ phải >= 1");
            }

            // Update fields
            existing.Name = price.Name;
            existing.BaseDailyPrice = price.BaseDailyPrice;
            existing.MonthMultiplier = price.MonthMultiplier;
            existing.PeakMultiplier = price.PeakMultiplier;
            existing.OvertimeHourlyPrice = price.OvertimeHourlyPrice;
            existing.ValidFrom = price.ValidFrom;
            existing.ValidTo = price.ValidTo;
            existing.IsActive = price.IsActive;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Cập nhật bảng giá thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật bảng giá: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeletePriceAsync(Guid priceId)
    {
        try
        {
            var price = await _context.Prices.FindAsync(priceId);
            if (price == null)
            {
                return ServiceResult.Fail("Bảng giá không tồn tại");
            }

            // Check if price is used in any contracts
            var isUsed = await _context.RentalContracts
                .AnyAsync(c => c.PriceId == priceId);

            if (isUsed)
            {
                return ServiceResult.Fail("Không thể xóa bảng giá đã được sử dụng trong hợp đồng");
            }

            _context.Prices.Remove(price);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Xóa bảng giá thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi xóa bảng giá: {ex.Message}");
        }
    }

    public async Task<ServiceResult> SetPriceActiveStatusAsync(Guid priceId, bool isActive)
    {
        try
        {
            var price = await _context.Prices.FindAsync(priceId);
            if (price == null)
            {
                return ServiceResult.Fail("Bảng giá không tồn tại");
            }

            price.IsActive = isActive;
            await _context.SaveChangesAsync();

            var status = isActive ? "kích hoạt" : "vô hiệu hóa";
            return ServiceResult.Ok($"Đã {status} bảng giá");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Lỗi khi cập nhật trạng thái: {ex.Message}");
        }
    }
}
