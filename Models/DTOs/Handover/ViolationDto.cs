using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO hiển thị thông tin vi phạm hợp đồng
/// </summary>
public class ViolationDto
{
    public Guid ViolationId { get; set; }
    public Guid ContractId { get; set; }
    public string? ContractCode { get; set; }

    public ViolationType ViolationType { get; set; }
    public string ViolationTypeName => ViolationType switch
    {
        ViolationType.Late => "Trả xe trễ",
        ViolationType.Damage => "Hư hỏng xe",
        ViolationType.Smoking => "Hút thuốc trong xe",
        ViolationType.LostKey => "Mất chìa khóa",
        _ => ViolationType.ToString()
    };

    public DateTime DetectedAt { get; set; }
    public string? Description { get; set; }

    public ViolationStatus Status { get; set; }
    public string StatusName => Status switch
    {
        ViolationStatus.Open => "Chờ xử lý",
        ViolationStatus.Resolved => "Đã xử lý",
        _ => Status.ToString()
    };

    public decimal PenaltyAmount { get; set; }
}
