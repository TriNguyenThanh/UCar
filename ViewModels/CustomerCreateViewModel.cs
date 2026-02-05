using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for creating new customer
/// Ánh xạ DFD 2.1: Đăng ký thông tin khách - input form
/// </summary>
public class CustomerCreateViewModel
{
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

    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4-100 ký tự")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? Dob { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    [Display(Name = "Địa chỉ")]
    public string? AddressText { get; set; }

    // Document information (CCCD/GPLX - từ DFD 2.1)
    [Required(ErrorMessage = "Loại giấy tờ là bắt buộc")]
    [Display(Name = "Loại giấy tờ")]
    public CustomerDocumentType DocumentType { get; set; }

    [Required(ErrorMessage = "Số giấy tờ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Số giấy tờ không được vượt quá 100 ký tự")]
    [Display(Name = "Số CCCD/GPLX/Passport")]
    public string DocumentNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp")]
    public DateTime? DocumentIssuedDate { get; set; }

    [StringLength(200, ErrorMessage = "Nơi cấp không được vượt quá 200 ký tự")]
    [Display(Name = "Nơi cấp")]
    public string? DocumentIssuedPlace { get; set; }

    [Display(Name = "Ảnh mặt trước")]
    public IFormFile? DocumentImageFront { get; set; }

    [Display(Name = "Ảnh mặt sau")]
    public IFormFile? DocumentImageBack { get; set; }
}
