namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái hợp đồng thuê xe
/// Luồng mới: Draft → PendingSigning → Active → Completed
/// </summary>
public enum RentalContractStatus
{
    /// <summary>Bản nháp (tạo sau khi xác nhận booking, chưa giao xe)</summary>
    Draft,

    /// <summary>Chờ ký (đã lập biên bản, in hợp đồng, chờ khách ký giấy)</summary>
    PendingSigning,

    /// <summary>Đang hoạt động (đã ký, đã thanh toán, đang thuê xe)</summary>
    Active,
    
    /// <summary>Đang thuê (đã giao xe, chờ trả)</summary>
    InProgress,
    
    /// <summary>Chờ quyết toán (đã nhận xe, chờ thanh toán phụ phí)</summary>
    PendingSettlement,
    
    /// <summary>Hoàn tất</summary>
    Completed,
    
    /// <summary>Vi phạm / Tranh chấp</summary>
    Disputed,
    
    /// <summary>Đã hủy</summary>
    Cancelled
}
