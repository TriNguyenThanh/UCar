namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái xe trong hệ thống cho thuê
/// </summary>
public enum VehicleStatus
{
    /// <summary>Sẵn sàng cho thuê</summary>
    Available,
    
    /// <summary>Đang được đặt (có booking)</summary>
    Reserved,
    
    /// <summary>Đang được thuê</summary>
    Renting,
    
    /// <summary>Đang bảo dưỡng</summary>
    Maintenance,
    
    /// <summary>Bị công an/CSGT giữ</summary>
    Impounded,
    
    /// <summary>Sự cố (hư hỏng, tai nạn)</summary>
    Incident,
    
    /// <summary>Ngừng khai thác</summary>
    Decommissioned
}
