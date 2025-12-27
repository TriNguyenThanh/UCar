using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO hiển thị thông tin sự cố
/// </summary>
public class IncidentDto
{
    public Guid IncidentId { get; set; }
    public IncidentType IncidentType { get; set; }
    public string IncidentTypeName => IncidentType switch
    {
        IncidentType.Accident => "Tai nạn",
        IncidentType.Theft => "Mất cắp",
        IncidentType.Fine => "Phạt nguội",
        IncidentType.Impound => "Tạm giữ xe",
        _ => IncidentType.ToString()
    };

    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;

    public Guid? ContractId { get; set; }
    public string? ContractCode { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public DateTime OccurredAt { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }

    public IncidentStatus Status { get; set; }
    public string StatusName => Status switch
    {
        IncidentStatus.New => "Mới",
        IncidentStatus.Processing => "Đang xử lý",
        IncidentStatus.Resolved => "Đã xử lý",
        _ => Status.ToString()
    };

    public decimal EstimatedCost { get; set; }
    public DateTime CreatedAt { get; set; }
}
