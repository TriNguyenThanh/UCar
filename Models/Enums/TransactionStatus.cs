namespace UCar.Models.Enums;

public enum TransactionStatus
{
    /// <summary>Đang chờ xử lý</summary>
    Pending,
    
    /// <summary>Đã thanh toán thành công</summary>
    Success,
    
    /// <summary>Thanh toán thất bại</summary>
    Failed,
    
    /// <summary>Đã hoàn tiền</summary>
    Refunded,
    
    /// <summary>Đã hủy</summary>
    Cancelled
}
