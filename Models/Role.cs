using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class Role
{
    [Key]
    public Guid RoleId { get; set; }

    [Required]
    public RoleCode Code { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
}
