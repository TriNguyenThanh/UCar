using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<PricingService> _logger;

    public PricingService(UCarDbContext context, ILogger<PricingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<Price>> GetPricesAsync(Guid? vehicleTypeId = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Prices
                .Include(p => p.VehicleType)
                .AsQueryable();

            // Apply filters
            if (vehicleTypeId.HasValue)
            {
                query = query.Where(p => p.VehicleTypeId == vehicleTypeId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply paging
            var items = await query
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.VehicleType.TypeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation($"Retrieved {items.Count} prices (page {page}/{Math.Ceiling(totalCount / (double)pageSize)})");

            return new PagedResult<Price>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prices");
            throw;
        }
    }

    public async Task<Price?> GetPriceByIdAsync(Guid priceId)
    {
        try
        {
            var price = await _context.Prices
                .Include(p => p.VehicleType)
                .FirstOrDefaultAsync(p => p.PriceId == priceId);

            if (price == null)
            {
                _logger.LogWarning($"Price not found: {priceId}");
            }

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving price {priceId}");
            throw;
        }
    }

    public async Task<Price?> GetActivePriceByVehicleTypeAsync(Guid vehicleTypeId)
    {
        try
        {
            var price = await _context.Prices
                .Include(p => p.VehicleType)
                .Where(p => p.VehicleTypeId == vehicleTypeId && p.IsActive)
                .FirstOrDefaultAsync();

            if (price == null)
            {
                _logger.LogWarning($"No active price found for vehicle type: {vehicleTypeId}");
            }

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving active price for vehicle type {vehicleTypeId}");
            throw;
        }
    }

    public async Task<ServiceResult<Guid>> CreatePriceAsync(Price price)
    {
        try
        {
            // Validation
            if (price.DailyBasePrice <= 0)
            {
                return ServiceResult<Guid>.Fail("Giá thuê phải lớn hơn 0");
            }

            // Check if vehicle type exists
            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == price.VehicleTypeId);

            if (!vehicleTypeExists)
            {
                return ServiceResult<Guid>.Fail("Loại xe không tồn tại");
            }

            price.PriceId = Guid.NewGuid();
            _context.Prices.Add(price);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created price: {price.PriceId} for vehicle type {price.VehicleTypeId}");

            return ServiceResult<Guid>.Ok(price.PriceId, "Tạo bảng giá thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price");
            return ServiceResult<Guid>.Fail("Lỗi khi tạo bảng giá: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UpdatePriceAsync(Guid priceId, Price price)
    {
        try
        {
            var existingPrice = await _context.Prices.FindAsync(priceId);
            if (existingPrice == null)
            {
                return ServiceResult.Fail("Bảng giá không tồn tại");
            }

            // Validation
            if (price.DailyBasePrice <= 0)
            {
                return ServiceResult.Fail("Giá thuê phải lớn hơn 0");
            }

            // Check if price is being used in any active contract
            var isUsedInContract = await _context.RentalContracts
                .AnyAsync(rc => rc.PriceId == priceId && rc.Status != Models.Enums.RentalContractStatus.Cancelled);

            if (isUsedInContract)
            {
                return ServiceResult.Fail("Không thể sửa bảng giá đang được sử dụng trong hợp đồng");
            }

            // Update fields
            existingPrice.Name = price.Name;
            existingPrice.DailyBasePrice = price.DailyBasePrice;
            existingPrice.MonthlyMultiplier = price.MonthlyMultiplier;
            existingPrice.HolidayMultiplier = price.HolidayMultiplier;
            existingPrice.WeekendMultiplier = price.WeekendMultiplier;
            existingPrice.OvertimeHourlyPrice = price.OvertimeHourlyPrice;
            existingPrice.DepositSuggest = price.DepositSuggest;
            existingPrice.IsActive = price.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated price: {priceId}");

            return ServiceResult.Ok("Cập nhật bảng giá thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating price {priceId}");
            return ServiceResult.Fail("Lỗi khi cập nhật bảng giá: " + ex.Message);
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

            // Check if price is being used in any contract
            var isUsedInContract = await _context.RentalContracts
                .AnyAsync(rc => rc.PriceId == priceId);

            if (isUsedInContract)
            {
                return ServiceResult.Fail("Không thể xóa bảng giá đã được sử dụng trong hợp đồng");
            }

            _context.Prices.Remove(price);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted price: {priceId}");

            return ServiceResult.Ok("Xóa bảng giá thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting price {priceId}");
            return ServiceResult.Fail("Lỗi khi xóa bảng giá: " + ex.Message);
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

            _logger.LogInformation($"Set price {priceId} active status to {isActive}");

            return ServiceResult.Ok($"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} bảng giá");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting price {priceId} active status");
            return ServiceResult.Fail("Lỗi khi cập nhật trạng thái bảng giá: " + ex.Message);
        }
    }
}
