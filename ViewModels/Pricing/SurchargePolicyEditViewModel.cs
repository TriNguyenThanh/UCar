using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel chỉnh sửa chính sách phụ phí
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class SurchargePolicyEditViewModel
{
    public Guid SurchargePolicyId { get; set; }
    
    [Required(ErrorMessage = "Tên chính sách là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên chính sách không được vượt quá 100 ký tự")]
    [Display(Name = "Tên chính sách")]
    public string PolicyName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn loại phụ phí")]
    [Display(Name = "Loại phụ phí")]
    public SurchargeType Type { get; set; }
    
    [Display(Name = "Loại xe (để trống = áp dụng tất cả)")]
    public Guid? VehicleTypeId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn phương thức tính")]
    [Display(Name = "Phương thức tính")]
    public SurchargeCalculationType CalculationType { get; set; }
    
    [Required(ErrorMessage = "Giá trị tính toán là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
    [Display(Name = "Giá trị (%, VNĐ hoặc VNĐ/đơn vị)")]
    public decimal Value { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải >= 0")]
    [Display(Name = "Số tiền tối thiểu (tùy chọn)")]
    public decimal? MinimumAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối đa phải >= 0")]
    [Display(Name = "Số tiền tối đa (tùy chọn)")]
    public decimal? MaximumAmount { get; set; }
    
    [StringLength(1000, ErrorMessage = "Điều kiện áp dụng không được vượt quá 1000 ký tự")]
    [Display(Name = "Điều kiện áp dụng")]
    public string? ApplicableCondition { get; set; }
    
    [Range(1, 100, ErrorMessage = "Mức ưu tiên phải từ 1-100")]
    [Display(Name = "Mức độ ưu tiên")]
    public int Priority { get; set; }
    
    [Required(ErrorMessage = "Ngày bắt đầu hiệu lực là bắt buộc")]
    [Display(Name = "Hiệu lực từ ngày")]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }
    
    [Display(Name = "Hiệu lực đến ngày")]
    [DataType(DataType.Date)]
    public DateTime? ValidTo { get; set; }
    
    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; }
    
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
