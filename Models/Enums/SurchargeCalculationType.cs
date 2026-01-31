namespace UCar.Models.Enums;

/// <summary>
/// Phương thức tính phụ phí
/// </summary>
public enum SurchargeCalculationType
{
    /// <summary>Tính theo phần trăm giá trị hợp đồng</summary>
    Percentage,

    /// <summary>Số tiền cố định</summary>
    FixedAmount,

    /// <summary>Tính theo đơn vị (VD: giá/giờ, giá/km)</summary>
    PerUnit
}
