using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO tạo mới vi phạm hợp đồng
/// </summary>
public class ViolationCreateDto
{
    [Required(ErrorMessage = "Vui lòng chọn loại vi phạm")]
    public ViolationType ViolationType { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả vi phạm")]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Mức phạt không hợp lệ")]
    public decimal PenaltyAmount { get; set; }
}
