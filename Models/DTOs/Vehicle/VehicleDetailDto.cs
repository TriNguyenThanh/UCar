using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO chi tiết xe đầy đủ
/// </summary>
public record VehicleDetailDto(
    Guid VehicleId,
    string PlateNo,
    Guid ModelId,
    string Make,
    string ModelName,
    Guid VehicleTypeId,
    string VehicleTypeName,
    string? Color,
    int ManufactureYear,
    int Seats,
    TransmissionType? Transmission,
    string? FuelType,
    VehicleStatus CurrentStatus,
    Guid BranchId,
    string BranchName,
    string? BranchAddress,
    decimal CurrentOdoKm,
    string? ImageFileName,
    IEnumerable<VehicleStatusHistoryDto>? RecentStatusHistory,
    IEnumerable<VehicleImageDto>? Images
);

/// <summary>
/// DTO cho ảnh xe
/// </summary>
public record VehicleImageDto(
    Guid ImageId,
    string ImageUrl,
    string? Caption,
    bool IsPrimary,
    DateTime UploadedAt
);
