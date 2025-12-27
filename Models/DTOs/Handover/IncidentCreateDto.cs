using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO tạo mới sự cố
/// </summary>
public class IncidentCreateDto
{
    [Required(ErrorMessage = "Vui lòng chọn loại sự cố")]
    public IncidentType IncidentType { get; set; }

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
}
