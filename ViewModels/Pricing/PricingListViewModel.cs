using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel hiển thị danh sách bảng giá (Index view)
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class PricingListViewModel
{
    public Guid PriceId { get; set; }
    
    [Display(Name = "Tên bảng giá")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Loại xe")]
    public string VehicleTypeName { get; set; } = string.Empty;
    
    public Guid VehicleTypeId { get; set; }
    
    [Display(Name = "Giá ngày cơ bản")]
    public decimal DailyBasePrice { get; set; }
    
    [Display(Name = "Hệ số tháng")]
    public decimal MonthlyMultiplier { get; set; }
    
    [Display(Name = "Hệ số lễ")]
    public decimal HolidayMultiplier { get; set; }
    
    [Display(Name = "Hệ số cuối tuần")]
    public decimal WeekendMultiplier { get; set; }
    
    [Display(Name = "Phụ phí quá giờ")]
    public decimal OvertimeHourlyPrice { get; set; }
    
    [Display(Name = "Tiền cọc đề xuất")]
    public decimal DepositSuggest { get; set; }
    
    [Display(Name = "Hiệu lực từ")]
    public DateTime ValidFrom { get; set; }
    
    [Display(Name = "Hiệu lực đến")]
    public DateTime? ValidTo { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
    
    [Display(Name = "Số hợp đồng sử dụng")]
    public int ContractCount { get; set; }
}

/// <summary>
/// ViewModel lọc và tìm kiếm bảng giá
/// </summary>
public class PricingSearchViewModel
{
    [Display(Name = "Loại xe")]
    public Guid? VehicleTypeId { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool? IsActive { get; set; }
    
    [Display(Name = "Tìm kiếm")]
    public string? SearchTerm { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
