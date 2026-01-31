using UCar.Models;
using UCar.Models.DTOs;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý bảng giá thuê xe
/// </summary>
public interface IPricingService
{
    // ===== PRICE CRUD =====
    
    /// <summary>
    /// Lấy danh sách bảng giá với filter và phân trang
    /// </summary>
    Task<PagedResult<Price>> GetPricesAsync(Guid? vehicleTypeId = null, bool? isActive = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy chi tiết bảng giá theo ID
    /// </summary>
    Task<Price?> GetPriceByIdAsync(Guid priceId);

    /// <summary>
    /// Lấy bảng giá đang active cho loại xe
    /// </summary>
    Task<Price?> GetActivePriceByVehicleTypeAsync(Guid vehicleTypeId);

    /// <summary>
    /// Tạo bảng giá mới
    /// </summary>
    Task<ServiceResult<Guid>> CreatePriceAsync(Price price);

    /// <summary>
    /// Cập nhật bảng giá
    /// </summary>
    Task<ServiceResult> UpdatePriceAsync(Guid priceId, Price price);

    /// <summary>
    /// Xóa bảng giá (chỉ nếu chưa được sử dụng)
    /// </summary>
    Task<ServiceResult> DeletePriceAsync(Guid priceId);

    /// <summary>
    /// Kích hoạt/vô hiệu hóa bảng giá
    /// </summary>
    Task<ServiceResult> SetPriceActiveStatusAsync(Guid priceId, bool isActive);
}
