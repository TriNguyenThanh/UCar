using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for customer details view
/// Ánh xạ DFD 2.2: Tra cứu lịch sử khách - kết quả chi tiết
/// </summary>
public class CustomerDetailsViewModel
{
    public Guid CustomerId { get; set; }
    
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }
    
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;
    
    [Display(Name = "Ngày sinh")]
    public DateTime? Dob { get; set; }
    
    [Display(Name = "Địa chỉ")]
    public string? AddressText { get; set; }
    
    [Display(Name = "Mức độ rủi ro")]
    public string? RiskLevel { get; set; }
    
    [Display(Name = "Danh sách đen")]
    public bool IsBlacklisted { get; set; }
    
    [Display(Name = "Trạng thái tài khoản")]
    public bool IsActive { get; set; }
    
    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; }
    
    [Display(Name = "Đăng nhập lần cuối")]
    public DateTime? LastLoginAt { get; set; }
    
    // Documents (CCCD/GPLX)
    public List<CustomerDocumentViewModel> Documents { get; set; } = new();
    
    // Rental history (từ DFD 2.2: Lịch sử thuê)
    public CustomerRentalHistoryViewModel RentalHistory { get; set; } = new();
}

/// <summary>
/// Customer document info for display
/// </summary>
public class CustomerDocumentViewModel
{
    public Guid DocId { get; set; }
    
    [Display(Name = "Loại giấy tờ")]
    public CustomerDocumentType DocType { get; set; }
    
    [Display(Name = "Số giấy tờ")]
    public string? DocNumber { get; set; }
    
    [Display(Name = "Ngày cấp")]
    public DateTime? IssuedDate { get; set; }
    
    [Display(Name = "Nơi cấp")]
    public string? IssuedPlace { get; set; }
    
    [Display(Name = "Đã xác minh")]
    public bool IsVerified { get; set; }
    
    public string? ImageFrontUrl { get; set; }
    public string? ImageBackUrl { get; set; }
}

/// <summary>
/// Rental history summary (từ DFD 2.2: Lịch sử thuê/Nợ xấu)
/// </summary>
public class CustomerRentalHistoryViewModel
{
    [Display(Name = "Tổng số đơn đặt")]
    public int TotalBookings { get; set; }
    
    [Display(Name = "Đơn hoàn thành")]
    public int CompletedBookings { get; set; }
    
    [Display(Name = "Đơn đã hủy")]
    public int CancelledBookings { get; set; }
    
    [Display(Name = "Tổng số hợp đồng")]
    public int TotalContracts { get; set; }
    
    [Display(Name = "Hợp đồng đang hoạt động")]
    public int ActiveContracts { get; set; }
    
    [Display(Name = "Tổng vi phạm")]
    public int TotalViolations { get; set; }
    
    [Display(Name = "Vi phạm chưa xử lý")]
    public int PendingViolations { get; set; }
    
    [Display(Name = "Tổng tiền đã chi trả")]
    public decimal TotalPaid { get; set; }
    
    [Display(Name = "Công nợ hiện tại")]
    public decimal CurrentDebt { get; set; }
    
    // Recent bookings
    public List<RecentBookingViewModel> RecentBookings { get; set; } = new();
}

/// <summary>
/// Recent booking item for customer history
/// </summary>
public class RecentBookingViewModel
{
    public Guid BookingId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
