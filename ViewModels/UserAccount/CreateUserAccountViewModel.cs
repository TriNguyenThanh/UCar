using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.UserAccount;

public class CreateUserAccountViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên đăng nhập tối đa 100 ký tự")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(200, ErrorMessage = "Email tối đa 200 ký tự")]
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }
    
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    [Display(Name = "Vai trò")]
    public Guid RoleId { get; set; }
    
    [Display(Name = "Kích hoạt tài khoản")]
    public bool IsActive { get; set; } = true;
    
    // For dropdown
    public List<RoleSelectItem> AvailableRoles { get; set; } = new();
}

public class RoleSelectItem
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoleCode Code { get; set; }
}
