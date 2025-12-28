namespace UCar.ViewModels;

public class UserProfileViewModel
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }

    // Customer specific
    public Guid? CustomerId { get; set; }
    public string? CustomerFullName { get; set; }
    public DateTime? CustomerDob { get; set; }
    public string? CustomerAddress { get; set; }
    public string? RiskLevel { get; set; }
    public bool? IsBlacklisted { get; set; }

    // Staff specific
    public Guid? StaffId { get; set; }
    public string? StaffFullName { get; set; }
    public string? StaffCode { get; set; }
    public string? Position { get; set; }
    public string? BranchName { get; set; }
}
