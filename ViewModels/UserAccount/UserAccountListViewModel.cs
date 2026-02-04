using UCar.Models.Enums;

namespace UCar.ViewModels.UserAccount;

public class UserAccountListViewModel
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public RoleCode RoleCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Linked profile info
    public string? LinkedProfileName { get; set; }
    public string? LinkedProfileType { get; set; } // "Customer" or "Staff"
}
