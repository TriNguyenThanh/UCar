using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho hiển thị danh sách xe (table view)
/// </summary>
public record VehicleListDto(
    Guid VehicleId,
    string PlateNo,
    string Make,
    string ModelName,
    string VehicleTypeName,
    string? Color,
    int ManufactureYear,
    VehicleStatus CurrentStatus,
    Guid BranchId,
    string BranchName,
    decimal CurrentOdoKm,
    int Seats,
    TransmissionType? Transmission,
    string? FuelType,
    string? ImageFileName
);
