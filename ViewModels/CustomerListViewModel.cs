using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for Customer list display (Index view)
/// Ánh xạ DFD 2.2: Tra cứu lịch sử khách - hiển thị danh sách
/// </summary>
public class CustomerListViewModel
{
    public Guid CustomerId { get; set; }
    
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }
    
    [Display(Name = "CCCD/CMND")]
    public string? IdCardNumber { get; set; }
    
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
    
    [Display(Name = "Tổng đơn đã thuê")]
    public int TotalBookings { get; set; }
}

/// <summary>
/// Search/Filter criteria for customer list
/// </summary>
public class CustomerSearchViewModel
{
    [Display(Name = "Tìm kiếm")]
    public string? SearchTerm { get; set; }
    
    [Display(Name = "Trạng thái")]
    public string? StatusFilter { get; set; }
    
    [Display(Name = "Mức độ rủi ro")]
    public string? RiskLevelFilter { get; set; }
    
    [Display(Name = "Sắp xếp theo")]
    public string? SortBy { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
