using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho hiển thị dòng xe (model)
/// </summary>
public record VehicleModelDto(
    Guid ModelId,
    Guid VehicleTypeId,
    string VehicleTypeName,
    string Make,
    string ModelName,
    int Seats,
    TransmissionType? Transmission,
    string? FuelType,
    string? ImageFileName,
    int VehicleCount
);

/// <summary>
/// DTO cho tạo dòng xe
/// </summary>
public class VehicleModelCreateDto
{
    [Required(ErrorMessage = "Vui lòng chọn loại xe")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = "Tên hãng xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên hãng xe tối đa 100 ký tự")]
    [Display(Name = "Hãng xe")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên dòng xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên dòng xe tối đa 100 ký tự")]
    [Display(Name = "Dòng xe")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số chỗ ngồi là bắt buộc")]
    [Range(1, 50, ErrorMessage = "Số chỗ ngồi từ 1-50")]
    [Display(Name = "Số chỗ ngồi")]
    public int Seats { get; set; } = 4;

    [Display(Name = "Hộp số")]
    public TransmissionType? Transmission { get; set; }

    [MaxLength(50, ErrorMessage = "Loại nhiên liệu tối đa 50 ký tự")]
    [Display(Name = "Nhiên liệu")]
    public string? FuelType { get; set; }

    [MaxLength(255, ErrorMessage = "Tên file ảnh tối đa 255 ký tự")]
    [Display(Name = "Ảnh dòng xe")]
    public string? ImageFileName { get; set; }
}

/// <summary>
/// DTO cho cập nhật dòng xe
/// </summary>
public class VehicleModelUpdateDto
{
    [Required(ErrorMessage = "Vui lòng chọn loại xe")]
    [Display(Name = "Loại xe")]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = "Tên hãng xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên hãng xe tối đa 100 ký tự")]
    [Display(Name = "Hãng xe")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên dòng xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên dòng xe tối đa 100 ký tự")]
    [Display(Name = "Dòng xe")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số chỗ ngồi là bắt buộc")]
    [Range(1, 50, ErrorMessage = "Số chỗ ngồi từ 1-50")]
    [Display(Name = "Số chỗ ngồi")]
    public int Seats { get; set; }

    [Display(Name = "Hộp số")]
    public TransmissionType? Transmission { get; set; }

    [MaxLength(50, ErrorMessage = "Loại nhiên liệu tối đa 50 ký tự")]
    [Display(Name = "Nhiên liệu")]
    public string? FuelType { get; set; }

    [MaxLength(255, ErrorMessage = "Tên file ảnh tối đa 255 ký tự")]
    [Display(Name = "Ảnh dòng xe")]
    public string? ImageFileName { get; set; }
}
