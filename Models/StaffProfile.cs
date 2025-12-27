using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

public class StaffProfile
{
    [Key]
    public Guid StaffId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid BranchId { get; set; }

    [Required]
    [MaxLength(50)]
    public string StaffCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Position { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public UserAccount UserAccount { get; set; } = null!;

    [ForeignKey(nameof(BranchId))]
    public Branch Branch { get; set; } = null!;
}
