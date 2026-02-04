using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.SurchargePolicy;

public class SurchargePolicyViewModel
{
    public Guid? SurchargePolicyId { get; set; }

    [Required(ErrorMessage = "Tên chính sách là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên chính sách không được quá 100 ký tự")]
    [Display(Name = "Tên chính sách")]
    public string PolicyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại xe là bắt buộc")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = "Loại phụ phí là bắt buộc")]
    [Display(Name = "Loại phụ phí")]
    public SurchargeType SurchargeType { get; set; }

    [Required(ErrorMessage = "Phương thức tính là bắt buộc")]
    [Display(Name = "Phương thức tính")]
    public SurchargeCalculationType CalculationType { get; set; }

    [Required(ErrorMessage = "Giá trị là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá trị")]
    public decimal Value { get; set; }

    [MaxLength(50, ErrorMessage = "Áp dụng trên không được quá 50 ký tự")]
    [Display(Name = "Áp dụng trên")]
    public string? AppliesTo { get; set; }

    [MaxLength(20, ErrorMessage = "Đơn vị không được quá 20 ký tự")]
    [Display(Name = "Đơn vị")]
    public string? Unit { get; set; }

    [MaxLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime ValidFrom { get; set; } = DateTime.Now;

    [Display(Name = "Ngày kết thúc")]
    public DateTime? ValidUntil { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;

    // Metadata (chỉ hiển thị trong Edit)
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
