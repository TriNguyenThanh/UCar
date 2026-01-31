using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel chỉnh sửa chính sách đặt cọc
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class DepositPolicyEditViewModel
{
    public Guid DepositPolicyId { get; set; }
    
    [Required(ErrorMessage = "Tên chính sách là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên chính sách không được vượt quá 100 ký tự")]
    [Display(Name = "Tên chính sách")]
    public string PolicyName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn loại xe")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn phương thức tính")]
    [Display(Name = "Phương thức tính")]
    public DepositCalculationType CalculationType { get; set; }
    
    [Required(ErrorMessage = "Giá trị tính toán là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
    [Display(Name = "Giá trị (% hoặc VNĐ)")]
    public decimal Value { get; set; }
    
    [Required(ErrorMessage = "Số tiền tối thiểu là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải >= 0")]
    [Display(Name = "Số tiền tối thiểu")]
    public decimal MinimumAmount { get; set; }
    
    [Required(ErrorMessage = "Số tiền tối đa là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối đa phải >= 0")]
    [Display(Name = "Số tiền tối đa")]
    public decimal MaximumAmount { get; set; }
    
    [StringLength(1000, ErrorMessage = "Điều kiện không được vượt quá 1000 ký tự")]
    [Display(Name = "Điều kiện hoàn cọc 100%")]
    public string? FullRefundCondition { get; set; }
    
    [StringLength(1000, ErrorMessage = "Điều kiện không được vượt quá 1000 ký tự")]
    [Display(Name = "Điều kiện hoàn cọc 50%")]
    public string? PartialRefundCondition { get; set; }
    
    [StringLength(1000, ErrorMessage = "Điều kiện không được vượt quá 1000 ký tự")]
    [Display(Name = "Điều kiện không hoàn cọc")]
    public string? NoRefundCondition { get; set; }
    
    [Required(ErrorMessage = "Số ngày xử lý hoàn cọc là bắt buộc")]
    [Range(1, 90, ErrorMessage = "Số ngày xử lý phải từ 1-90 ngày")]
    [Display(Name = "Số ngày xử lý hoàn cọc")]
    public int RefundProcessingDays { get; set; }
    
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
