using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.UserAccount;

public class EditUserAccountViewModel
{
    public Guid UserId { get; set; }
    
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
    
    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    [Display(Name = "Vai trò")]
    public Guid RoleId { get; set; }
    
    [Display(Name = "Kích hoạt tài khoản")]
    public bool IsActive { get; set; }
    
    // For dropdown
    public List<RoleSelectItem> AvailableRoles { get; set; } = new();
    
    // Read-only info
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LinkedProfileName { get; set; }
    public string? LinkedProfileType { get; set; }
}
