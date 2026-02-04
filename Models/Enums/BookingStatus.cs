namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái đơn đặt xe
/// </summary>
public enum BookingStatus
{
    /// <summary>Chờ xác nhận (khách vừa đặt)</summary>
    Pending,
    
    /// <summary>Đã xác nhận (nhân viên duyệt)</summary>
    Confirmed,
    
    /// <summary>Đã đặt cọc</summary>
    Deposited,
    
    /// <summary>Đang thực hiện (đã tạo hợp đồng, đang thuê)</summary>
    InProgress,
    
    /// <summary>Hoàn thành (đã trả xe và quyết toán)</summary>
    Completed,
    
    /// <summary>Đã hủy (bởi khách hoặc hệ thống)</summary>
    Cancelled,
    
    /// <summary>Từ chối (bởi nhân viên)</summary>
    Rejected,
    
    /// <summary>Hết hạn (không xác nhận kịp)</summary>
    Expired
}
