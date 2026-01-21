namespace UCar.Models.Enums;

/// <summary>
/// Trạng thái nhiệm vụ vận hành
/// </summary>
public enum OpTaskStatus
{
    /// <summary>Mới tạo, chưa phân công</summary>
    New,

    /// <summary>Đã phân công cho nhân viên</summary>
    Assigned,

    /// <summary>Đang thực hiện</summary>
    InProgress,

    /// <summary>Hoàn thành</summary>
    Completed,

    /// <summary>Đã hủy</summary>
    Cancelled,

    /// <summary>Thất bại</summary>
    Failed
}
