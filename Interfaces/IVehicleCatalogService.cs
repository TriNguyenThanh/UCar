using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý danh mục xe (loại xe, dòng xe)
/// </summary>
public interface IVehicleCatalogService
{
    #region Vehicle Types

    /// <summary>
    /// Lấy danh sách loại xe
    /// </summary>
    Task<IEnumerable<VehicleTypeDto>> GetAllVehicleTypesAsync();

    /// <summary>
    /// Lấy loại xe theo ID
    /// </summary>
    Task<VehicleTypeDto?> GetVehicleTypeByIdAsync(Guid id);

    /// <summary>
    /// Tạo loại xe mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateVehicleTypeAsync(VehicleTypeCreateDto dto);

    /// <summary>
    /// Cập nhật loại xe
    /// </summary>
    Task<ServiceResult> UpdateVehicleTypeAsync(Guid id, VehicleTypeUpdateDto dto);

    /// <summary>
    /// Xóa loại xe
    /// </summary>
    Task<ServiceResult> DeleteVehicleTypeAsync(Guid id);

    #endregion

    #region Vehicle Models

    /// <summary>
    /// Lấy danh sách dòng xe, có thể lọc theo loại xe
    /// </summary>
    Task<IEnumerable<VehicleModelDto>> GetAllVehicleModelsAsync(Guid? vehicleTypeId = null);

    /// <summary>
    /// Lấy dòng xe theo ID
    /// </summary>
    Task<VehicleModelDto?> GetVehicleModelByIdAsync(Guid id);

    /// <summary>
    /// Tạo dòng xe mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateVehicleModelAsync(VehicleModelCreateDto dto);

    /// <summary>
    /// Cập nhật dòng xe
    /// </summary>
    Task<ServiceResult> UpdateVehicleModelAsync(Guid id, VehicleModelUpdateDto dto);

    /// <summary>
    /// Xóa dòng xe
    /// </summary>
    Task<ServiceResult> DeleteVehicleModelAsync(Guid id);

    #endregion
}
