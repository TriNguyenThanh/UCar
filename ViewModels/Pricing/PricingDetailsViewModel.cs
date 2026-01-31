using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel hiển thị chi tiết bảng giá
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class PricingDetailsViewModel
{
    public Guid PriceId { get; set; }
    
    [Display(Name = "Tên bảng giá")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Loại xe")]
    public string VehicleTypeName { get; set; } = string.Empty;
    
    public Guid VehicleTypeId { get; set; }
    
    [Display(Name = "Giá ngày cơ bản")]
    public decimal DailyBasePrice { get; set; }
    
    [Display(Name = "Giá tháng (tính toán)")]
    public decimal MonthlyPrice { get; set; }
    
    [Display(Name = "Giá ngày lễ (tính toán)")]
    public decimal HolidayDailyPrice { get; set; }
    
    [Display(Name = "Giá cuối tuần (tính toán)")]
    public decimal WeekendDailyPrice { get; set; }
    
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
    
    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
    
    [Display(Name = "Số hợp đồng sử dụng")]
    public int ContractCount { get; set; }
    
    [Display(Name = "Tổng doanh thu")]
    public decimal TotalRevenue { get; set; }
    
    /// <summary>Danh sách hợp đồng sử dụng bảng giá này (tối đa 10)</summary>
    public List<PriceContractUsageViewModel> RecentContracts { get; set; } = new();
}

/// <summary>
/// ViewModel hiển thị thông tin hợp đồng sử dụng bảng giá
/// </summary>
public class PriceContractUsageViewModel
{
    public Guid ContractId { get; set; }
    
    [Display(Name = "Mã hợp đồng")]
    public string ContractCode { get; set; } = string.Empty;
    
    [Display(Name = "Khách hàng")]
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Xe")]
    public string VehiclePlateNo { get; set; } = string.Empty;
    
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }
    
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }
    
    [Display(Name = "Tổng tiền")]
    public decimal TotalAmount { get; set; }
}
