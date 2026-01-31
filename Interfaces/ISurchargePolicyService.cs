using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý chính sách phụ phí
/// </summary>
public interface ISurchargePolicyService
{
    // ===== SURCHARGE POLICY CRUD =====
    
    /// <summary>
    /// Lấy danh sách chính sách phụ phí
    /// </summary>
    Task<PagedResult<SurchargePolicy>> GetSurchargePoliciesAsync(
        SurchargeType? type = null, 
        Guid? vehicleTypeId = null, 
        bool? isActive = null, 
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// Lấy chi tiết chính sách phụ phí theo ID
    /// </summary>
    Task<SurchargePolicy?> GetSurchargePolicyByIdAsync(Guid policyId);

    /// <summary>
    /// Lấy tất cả chính sách phụ phí đang active theo loại
    /// </summary>
    Task<List<SurchargePolicy>> GetActiveSurchargePoliciesByTypeAsync(SurchargeType type, Guid? vehicleTypeId = null);

    /// <summary>
    /// Tạo chính sách phụ phí mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateSurchargePolicyAsync(SurchargePolicy policy);

    /// <summary>
    /// Cập nhật chính sách phụ phí
    /// </summary>
    Task<ServiceResult> UpdateSurchargePolicyAsync(Guid policyId, SurchargePolicy policy);

    /// <summary>
    /// Xóa chính sách phụ phí
    /// </summary>
    Task<ServiceResult> DeleteSurchargePolicyAsync(Guid policyId);

    /// <summary>
    /// Kích hoạt/vô hiệu hóa chính sách
    /// </summary>
    Task<ServiceResult> SetSurchargePolicyActiveStatusAsync(Guid policyId, bool isActive);

    // ===== SURCHARGE CALCULATION =====
    
    /// <summary>
    /// Tính phụ phí quá giờ
    /// </summary>
    /// <param name="vehicleTypeId">Loại xe</param>
    /// <param name="overtimeHours">Số giờ quá hạn</param>
    Task<decimal> CalculateOvertimeSurchargeAsync(Guid vehicleTypeId, decimal overtimeHours);

    /// <summary>
    /// Tính phụ phí quá km
    /// </summary>
    /// <param name="vehicleTypeId">Loại xe</param>
    /// <param name="extraKilometers">Số km vượt quá giới hạn</param>
    Task<decimal> CalculateExtraKilometerSurchargeAsync(Guid vehicleTypeId, decimal extraKilometers);

    /// <summary>
    /// Tính phụ phí ngày lễ/cuối tuần
    /// </summary>
    /// <param name="vehicleTypeId">Loại xe</param>
    /// <param name="rentalAmount">Giá trị hợp đồng</param>
    Task<decimal> CalculateHolidaySurchargeAsync(Guid vehicleTypeId, decimal rentalAmount);

    /// <summary>
    /// Lấy phụ phí cố định theo loại
    /// </summary>
    /// <param name="type">Loại phụ phí (DeliveryService, CleaningFee, DriverService, etc.)</param>
    /// <param name="vehicleTypeId">Loại xe (optional)</param>
    Task<decimal> GetFixedSurchargeAmountAsync(SurchargeType type, Guid? vehicleTypeId = null);

    /// <summary>
    /// Tính tổng phụ phí cho một hợp đồng dựa trên các điều kiện
    /// </summary>
    /// <param name="vehicleTypeId">Loại xe</param>
    /// <param name="rentalAmount">Giá trị hợp đồng</param>
    /// <param name="overtimeHours">Số giờ quá hạn</param>
    /// <param name="extraKm">Số km vượt</param>
    /// <param name="needsCleaning">Cần vệ sinh đặc biệt</param>
    /// <param name="isHolidayWeekend">Thuê ngày lễ/cuối tuần</param>
    /// <param name="hasDelivery">Có giao xe tận nơi</param>
    /// <param name="hasDriver">Có thuê lái xe</param>
    /// <param name="rentalDays">Số ngày thuê (cho dịch vụ theo ngày)</param>
    Task<decimal> CalculateTotalSurchargesAsync(
        Guid vehicleTypeId,
        decimal rentalAmount,
        decimal overtimeHours = 0,
        decimal extraKm = 0,
        bool needsCleaning = false,
        bool isHolidayWeekend = false,
        bool hasDelivery = false,
        bool hasDriver = false,
        int rentalDays = 0);
}
