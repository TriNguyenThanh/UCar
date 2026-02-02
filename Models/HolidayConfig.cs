using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Cấu hình ngày lễ để tính giá peak
/// </summary>
public class HolidayConfig
{
    [Key]
    public Guid HolidayId { get; set; }

    [Required]
    [MaxLength(200)]
    public string HolidayName { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int Year { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    // Navigation
    [ForeignKey(nameof(CreatedBy))]
    public UserAccount? CreatedByUser { get; set; }
}
