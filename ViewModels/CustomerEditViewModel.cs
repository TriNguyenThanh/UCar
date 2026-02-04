using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for editing existing customer
/// Bổ sung hợp lý: Update customer information
/// </summary>
public class CustomerEditViewModel
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 hoặc +84 và có 10-11 số")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? Dob { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    [Display(Name = "Địa chỉ")]
    public string? AddressText { get; set; }

    [StringLength(50)]
    [Display(Name = "Mức độ rủi ro")]
    public string? RiskLevel { get; set; }

    [Display(Name = "Danh sách đen")]
    public bool IsBlacklisted { get; set; }

    [Display(Name = "Tài khoản hoạt động")]
    public bool IsActive { get; set; }

    // For concurrency check
    public DateTime CreatedAt { get; set; }
}
