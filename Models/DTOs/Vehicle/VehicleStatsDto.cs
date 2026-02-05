using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO chứa thống kê tình trạng xe
/// </summary>
public class VehicleStatsDto
{
    /// <summary>Tổng số xe</summary>
    public int TotalCount { get; set; }
    
    /// <summary>Số xe sẵn sàng</summary>
    public int AvailableCount { get; set; }
    
    /// <summary>Số xe đang cho thuê</summary>
    public int RentingCount { get; set; }
    
    /// <summary>Số xe bảo dưỡng</summary>
    public int MaintenanceCount { get; set; }
    
    /// <summary>Số xe đã đặt</summary>
    public int ReservedCount { get; set; }
    
    /// <summary>Số xe bị giữ</summary>
    public int ImpoundedCount { get; set; }
    
    /// <summary>Số xe gặp sự cố</summary>
    public int IncidentCount { get; set; }
}