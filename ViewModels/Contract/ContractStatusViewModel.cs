using UCar.Models.Enums;

namespace UCar.ViewModels.Contract;

/// <summary>
/// ViewModel trả về trạng thái Contract (dùng cho API status endpoint)
/// Phục vụ tích hợp giữa Contract module và Handover module
/// </summary>
public class ContractStatusViewModel
{
    /// <summary>
    /// ID của Contract
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Mã hợp đồng (ví dụ: HD-001234)
    /// </summary>
    public string ContractCode { get; set; } = string.Empty;

    /// <summary>
    /// ID của Booking liên kết (null nếu walk-in)
    /// </summary>
    public Guid? BookingId { get; set; }

    /// <summary>
    /// Trạng thái hiện tại của Contract (enum value)
    /// </summary>
    public RentalContractStatus Status { get; set; }

    /// <summary>
    /// Trạng thái hiển thị (text tiếng Việt)
    /// </summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Contract đã được khách hàng ký hay chưa
    /// </summary>
    public bool IsSigned { get; set; }

    /// <summary>
    /// Thời điểm khách hàng ký (nếu đã ký)
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Contract đã được nhân viên xác nhận hay chưa
    /// </summary>
    public bool IsConfirmed { get; set; }

    /// <summary>
    /// Thời điểm nhân viên xác nhận (nếu đã xác nhận)
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// ✅ QUAN TRỌNG: Contract sẵn sàng để giao xe (Handover)
    /// Handover module dùng field này để quyết định có cho phép tạo HandoverRecord hay không
    /// Điều kiện: IsSigned == true AND Booking chưa bị hủy
    /// </summary>
    public bool IsReadyForHandover { get; set; }

    /// <summary>
    /// Booking liên kết đã bị hủy hay chưa
    /// </summary>
    public bool IsBookingCancelled { get; set; }

    /// <summary>
    /// Tổng chi phí ước tính
    /// </summary>
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Ngày bắt đầu dự kiến
    /// </summary>
    public DateTime PlannedStart { get; set; }

    /// <summary>
    /// Ngày kết thúc dự kiến
    /// </summary>
    public DateTime PlannedEnd { get; set; }

    /// <summary>
    /// Thông báo mô tả trạng thái (dùng để hiển thị cho user)
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
