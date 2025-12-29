namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái hợp đồng thuê xe
/// </summary>
public enum RentalContractStatus
{
    /// <summary>Đang hoạt động (đã tạo, chờ giao xe)</summary>
    Active,
    
    /// <summary>Chờ giao xe</summary>
    AwaitingDelivery,
    
    /// <summary>Đang thuê (đã giao xe, chờ trả)</summary>
    InProgress,
    
    /// <summary>Chờ nhận xe</summary>
    AwaitingReturn,
    
    /// <summary>Chờ quyết toán (đã nhận xe, chờ thanh toán phụ phí)</summary>
    PendingSettlement,
    
    /// <summary>Hoàn tất</summary>
    Completed,
    
    /// <summary>Vi phạm</summary>
    Violation,
    
    /// <summary>Đã hủy</summary>
    Cancelled
}
