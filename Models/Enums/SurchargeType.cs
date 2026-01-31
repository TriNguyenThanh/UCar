namespace UCar.Models.Enums;

/// <summary>
/// Loại phụ phí
/// </summary>
public enum SurchargeType
{
    /// <summary>Phụ phí quá giờ</summary>
    Overtime,

    /// <summary>Phụ phí quá km</summary>
    ExtraKilometer,

    /// <summary>Phụ phí giao xe tận nơi</summary>
    DeliveryService,

    /// <summary>Phụ phí thuê lái xe</summary>
    DriverService,

    /// <summary>Phụ phí ngày lễ/cuối tuần</summary>
    HolidayWeekend,

    /// <summary>Phụ phí thuê theo mùa cao điểm</summary>
    SeasonalPeak,

    /// <summary>Phụ phí vệ sinh xe bẩn</summary>
    CleaningFee,

    /// <summary>Phụ phí trả xe muộn</summary>
    LateReturn,

    /// <summary>Phụ phí khác</summary>
    Other
}
