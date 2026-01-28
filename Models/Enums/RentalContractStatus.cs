namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái hợp đồng thuê xe
/// </summary>
public enum RentalContractStatus
{
    /// <summary>Bản nháp (chưa hoàn tất, chưa ký)</summary>
    Draft,

    /// <summary>Chờ xác nhận/ký (đã tạo, chờ khách ký)</summary>
    Pending,

    /// <summary>Đã ký/Xác nhận (khách đã đồng ý điều khoản)</summary>
    Signed,

    /// <summary>Đang hoạt động (đã xác nhận, chờ giao xe)</summary>
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
