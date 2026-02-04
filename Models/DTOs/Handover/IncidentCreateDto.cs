using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO tạo mới sự cố
/// </summary>
public class IncidentCreateDto
{
    public IncidentType? IncidentType { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời gian xảy ra")]
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả sự cố")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Chi phí ước tính không hợp lệ")]
    public decimal EstimatedCost { get; set; }
    
    // Fine Detail (khi IncidentType = Fine)
    public FineDetailDto? FineDetail { get; set; }
    
    // Impound Detail (khi IncidentType = Impound)
    public ImpoundDetailDto? ImpoundDetail { get; set; }
}

/// <summary>
/// DTO chi tiết phạt nguội
/// </summary>
public class FineDetailDto
{
    [MaxLength(100)]
    public string? TicketNumber { get; set; }
    
    [MaxLength(200)]
    public string? AgencyName { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? FineAmount { get; set; }
}

/// <summary>
/// DTO chi tiết tạm giữ xe
/// </summary>
public class ImpoundDetailDto
{
    [MaxLength(500)]
    public string? ImpoundLotAddress { get; set; }
    
    public DateTime ImpoundedAt { get; set; } = DateTime.Now;
    
    public DateTime? ReleasedAt { get; set; }
}
