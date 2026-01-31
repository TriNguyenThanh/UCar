using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel chỉnh sửa bảng giá
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class PricingEditViewModel
{
    public Guid PriceId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn loại xe")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }
    
    [Required(ErrorMessage = "Tên bảng giá là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên bảng giá không được vượt quá 100 ký tự")]
    [Display(Name = "Tên bảng giá")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng nhập giá thuê ngày")]
    [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
    [Display(Name = "Giá thuê ngày cơ bản")]
    public decimal DailyBasePrice { get; set; }
    
    [Required(ErrorMessage = "Hệ số tháng là bắt buộc")]
    [Range(0.01, 0.99, ErrorMessage = "Hệ số tháng phải từ 0.01 đến 0.99 (giảm giá 1%-99%)")]
    [Display(Name = "Hệ số giảm giá thuê tháng")]
    public decimal MonthlyMultiplier { get; set; }
    
    [Required(ErrorMessage = "Hệ số lễ là bắt buộc")]
    [Range(1.01, 3.0, ErrorMessage = "Hệ số lễ phải từ 1.01 đến 3.0 (tăng giá 1%-200%)")]
    [Display(Name = "Hệ số tăng giá ngày lễ")]
    public decimal HolidayMultiplier { get; set; }
    
    [Required(ErrorMessage = "Hệ số cuối tuần là bắt buộc")]
    [Range(1.01, 2.0, ErrorMessage = "Hệ số cuối tuần phải từ 1.01 đến 2.0 (tăng giá 1%-100%)")]
    [Display(Name = "Hệ số tăng giá cuối tuần")]
    public decimal WeekendMultiplier { get; set; }
    
    [Required(ErrorMessage = "Phụ phí quá giờ là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Phụ phí quá giờ phải >= 0")]
    [Display(Name = "Phụ phí quá giờ (VNĐ/giờ)")]
    public decimal OvertimeHourlyPrice { get; set; }
    
    [Required(ErrorMessage = "Tiền cọc đề xuất là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc đề xuất phải >= 0")]
    [Display(Name = "Tiền cọc đề xuất")]
    public decimal DepositSuggest { get; set; }
    
    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; }
    
    /// <summary>Thông tin để kiểm tra có đang được sử dụng không</summary>
    public bool IsUsedInContracts { get; set; }
    
    [Display(Name = "Số hợp đồng đang sử dụng")]
    public int ContractCount { get; set; }
}
