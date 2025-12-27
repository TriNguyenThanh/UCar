using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý xe (CRUD và truy vấn)
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Lấy danh sách xe với filter và phân trang
    /// </summary>
    Task<PagedResult<VehicleListDto>> GetAllVehiclesAsync(VehicleFilterDto? filter = null);

    /// <summary>
    /// Lấy chi tiết xe theo ID
    /// </summary>
    Task<VehicleDetailDto?> GetVehicleByIdAsync(Guid id);

    /// <summary>
    /// Tạo xe mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateVehicleAsync(VehicleCreateDto dto, Guid userId);

    /// <summary>
    /// Cập nhật thông tin xe
    /// </summary>
    Task<ServiceResult> UpdateVehicleAsync(Guid id, VehicleUpdateDto dto, Guid userId);

    /// <summary>
    /// Xóa xe (soft delete hoặc kiểm tra ràng buộc)
    /// </summary>
    Task<ServiceResult> DeleteVehicleAsync(Guid id);

    /// <summary>
    /// Lấy danh sách xe khả dụng trong khoảng thời gian
    /// </summary>
    Task<IEnumerable<VehicleListDto>> GetAvailableVehiclesAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Kiểm tra biển số đã tồn tại chưa
    /// </summary>
    Task<bool> IsPlateNoExistsAsync(string plateNo, Guid? excludeId = null);

    /// <summary>
    /// Lấy danh sách hãng xe (distinct)
    /// </summary>
    Task<IEnumerable<string>> GetMakesAsync();

    /// <summary>
    /// Lấy danh sách chi nhánh cho dropdown
    /// </summary>
    Task<IEnumerable<(Guid Id, string Name)>> GetBranchesAsync();
}
