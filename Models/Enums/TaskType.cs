namespace UCar.Models.Enums;

/// <summary>
/// Loại nhiệm vụ vận hành
/// </summary>
public enum TaskType
{
    /// <summary>Giao xe cho khách</summary>
    Delivery,

    /// <summary>Nhận xe từ khách</summary>
    Return,

    /// <summary>Bảo dưỡng/sửa chữa</summary>
    Maintenance,

    /// <summary>Cứu hộ khẩn cấp</summary>
    Rescue,

    /// <summary>Kiểm tra xe</summary>
    Inspection
}
