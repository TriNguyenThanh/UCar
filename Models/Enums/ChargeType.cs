namespace UCar.Models.Enums;

/// <summary>
/// Loại phụ phí hợp đồng
/// </summary>
public enum ChargeType
{
    /// <summary>Phí trễ hạn</summary>
    LateFee,
    
    /// <summary>Phí vệ sinh</summary>
    CleaningFee,
    
    /// <summary>Phí hư hỏng</summary>
    DamageFee,
    
    /// <summary>Phí nhiên liệu thiếu</summary>
    FuelShortage,
    
    /// <summary>Phí mất phụ kiện</summary>
    AccessoryLoss,
    
    /// <summary>Phí quá giờ</summary>
    OvertimeFee,
    
    /// <summary>Phí cầu đường</summary>
    TollFee
}
