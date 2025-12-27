using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Interfaces;

/// <summary>
/// Interface cho quản lý trạng thái xe
/// </summary>
public interface IVehicleStatusService
{
    /// <summary>
    /// Thay đổi trạng thái xe với validation state machine
    /// </summary>
    Task<ServiceResult> ChangeStatusAsync(VehicleStatusChangeDto dto, Guid userId);

    /// <summary>
    /// Lấy lịch sử trạng thái xe
    /// </summary>
    Task<IEnumerable<VehicleStatusHistoryDto>> GetStatusHistoryAsync(Guid vehicleId, int? limit = null);

    /// <summary>
    /// Kiểm tra có thể chuyển sang trạng thái mới không
    /// </summary>
    Task<bool> CanChangeStatusAsync(Guid vehicleId, VehicleStatus newStatus);

    /// <summary>
    /// Lấy danh sách trạng thái có thể chuyển đến từ trạng thái hiện tại
    /// </summary>
    Task<IEnumerable<VehicleStatus>> GetAllowedTransitionsAsync(Guid vehicleId);

    /// <summary>
    /// Lấy trạng thái hiện tại của xe
    /// </summary>
    Task<VehicleStatus?> GetCurrentStatusAsync(Guid vehicleId);
}
