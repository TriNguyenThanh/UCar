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

    /// <summary>Phụ phí ngày lễ/cuối tuần</summary>
    HolidaySurcharge,

    /// <summary>Phí giao xe tận nơi</summary>
    DeliveryService,

    /// <summary>Phí vệ sinh đặc biệt</summary>
    CleaningFee,

    /// <summary>Phí thuê lái xe</summary>
    DriverService,

    /// <summary>Phí bảo hiểm bổ sung</summary>
    InsuranceExtra,

    /// <summary>Phí nhiên liệu không đủ</summary>
    FuelShortage,

    /// <summary>Phí khác</summary>
    Other
}

/// <summary>
/// Phương thức tính phụ phí
/// </summary>
public enum SurchargeCalculationType
{
    /// <summary>Số tiền cố định</summary>
    FixedAmount,

    /// <summary>Phần trăm (%) trên giá trị thuê</summary>
    Percentage,

    /// <summary>Theo đơn vị (giờ, km, ngày)</summary>
    PerUnit
}
