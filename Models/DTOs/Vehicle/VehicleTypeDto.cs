using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho hiển thị loại xe
/// </summary>
public record VehicleTypeDto(
    Guid VehicleTypeId,
    string TypeName,
    string? Description,
    int VehicleModelCount
);

/// <summary>
/// DTO cho tạo loại xe
/// </summary>
public class VehicleTypeCreateDto
{
    [Required(ErrorMessage = "Tên loại xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên loại xe tối đa 100 ký tự")]
    [Display(Name = "Tên loại xe")]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}

/// <summary>
/// DTO cho cập nhật loại xe
/// </summary>
public class VehicleTypeUpdateDto
{
    [Required(ErrorMessage = "Tên loại xe là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên loại xe tối đa 100 ký tự")]
    [Display(Name = "Tên loại xe")]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
