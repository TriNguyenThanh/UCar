using UCar.Models.Enums;

namespace UCar.ViewModels.UserAccount;

public class UserAccountDetailsViewModel
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public RoleCode RoleCode { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Linked Customer info
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    
    // Linked Staff info
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? StaffPosition { get; set; }
    public string? BranchName { get; set; }
}
