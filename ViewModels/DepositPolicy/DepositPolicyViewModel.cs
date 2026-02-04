using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.DepositPolicy;

public class DepositPolicyViewModel
{
    public Guid DepositPolicyId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chính sách")]
    [Display(Name = "Tên chính sách")]
    [StringLength(100, ErrorMessage = "Tên chính sách không quá 100 ký tự")]
    public string PolicyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại xe")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }

    // ===== CỌC TRÁCH NHIỆM =====
    
    [Required(ErrorMessage = "Vui lòng nhập tiền cọc trách nhiệm")]
    [Display(Name = "Tiền cọc trách nhiệm (VNĐ)")]
    [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc trách nhiệm phải >= 0")]
    public decimal ResponsibilityDepositAmount { get; set; }

    // ===== CỌC THUÊ XE =====
    
    [Required(ErrorMessage = "Vui lòng chọn phương thức tính cọc thuê")]
    [Display(Name = "Phương thức tính cọc thuê")]
    public DepositCalculationType RentalDepositCalculationType { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá trị")]
    [Display(Name = "Giá trị (% hoặc số tiền)")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá trị phải >= 0")]
    public decimal RentalDepositValue { get; set; }

    [Display(Name = "Số tiền tối thiểu (VNĐ)")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải >= 0")]
    public decimal RentalDepositMinimum { get; set; }

    [Display(Name = "Số tiền tối đa (VNĐ)")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối đa phải >= 0")]
    public decimal RentalDepositMaximum { get; set; }

    // ===== ĐIỀU KIỆN HOÀN CỌC =====
    
    [Display(Name = "Điều kiện hoàn 100%")]
    [StringLength(1000, ErrorMessage = "Không quá 1000 ký tự")]
    public string? FullRefundCondition { get; set; }

    [Display(Name = "Điều kiện hoàn 50%")]
    [StringLength(1000, ErrorMessage = "Không quá 1000 ký tự")]
    public string? PartialRefundCondition { get; set; }

    [Display(Name = "Điều kiện không hoàn")]
    [StringLength(1000, ErrorMessage = "Không quá 1000 ký tự")]
    public string? NoRefundCondition { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số ngày xử lý hoàn cọc")]
    [Display(Name = "Số ngày xử lý hoàn cọc")]
    [Range(1, 90, ErrorMessage = "Số ngày xử lý từ 1-90 ngày")]
    public int RefundProcessingDays { get; set; }

    // ===== THỜI GIAN HIỆU LỰC =====
    
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [Display(Name = "Có hiệu lực từ ngày")]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [Display(Name = "Có hiệu lực đến ngày")]
    [DataType(DataType.Date)]
    public DateTime? ValidTo { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; }

    [Display(Name = "Mô tả")]
    [StringLength(500, ErrorMessage = "Mô tả không quá 500 ký tự")]
    public string? Description { get; set; }

    // For display only
    public string? VehicleTypeName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
