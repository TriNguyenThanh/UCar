using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho thay đổi trạng thái xe
/// </summary>
public class VehicleStatusChangeDto
{
    [Required(ErrorMessage = "Vui lòng chọn xe")]
    public Guid VehicleId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái mới")]
    [Display(Name = "Trạng thái mới")]
    public VehicleStatus NewStatus { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

/// <summary>
/// DTO cho lịch sử trạng thái xe
/// </summary>
public record VehicleStatusHistoryDto(
    Guid VshId,
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    Guid ChangedBy,
    string? ChangedByName,
    string? Note
);

/// <summary>
/// Helper class cho việc hiển thị trạng thái xe
/// </summary>
public static class VehicleStatusHelper
{
    public static string GetDisplayName(VehicleStatus status) => status switch
    {
        VehicleStatus.Available => "Sẵn sàng",
        VehicleStatus.Reserved => "Đang đặt",
        VehicleStatus.Renting => "Đang thuê",
        VehicleStatus.Maintenance => "Bảo dưỡng",
        VehicleStatus.Impounded => "Bị giữ",
        VehicleStatus.Incident => "Sự cố",
        VehicleStatus.Decommissioned => "Ngừng khai thác",
        _ => status.ToString()
    };

    public static string GetChipClass(VehicleStatus status) => status switch
    {
        VehicleStatus.Available => "chip-success",
        VehicleStatus.Reserved => "chip-info",
        VehicleStatus.Renting => "chip-warning",
        VehicleStatus.Maintenance => "chip-error",
        VehicleStatus.Impounded => "chip-error",
        VehicleStatus.Incident => "chip-error",
        VehicleStatus.Decommissioned => "",
        _ => ""
    };

    public static string GetIcon(VehicleStatus status) => status switch
    {
        VehicleStatus.Available => "check_circle",
        VehicleStatus.Reserved => "event",
        VehicleStatus.Renting => "directions_car",
        VehicleStatus.Maintenance => "build",
        VehicleStatus.Impounded => "gavel",
        VehicleStatus.Incident => "warning",
        VehicleStatus.Decommissioned => "block",
        _ => "help"
    };
}
