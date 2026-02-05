using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho tạo xe mới
/// </summary>
public class VehicleCreateDto
{
    [Required(ErrorMessage = "Biển số xe là bắt buộc")]
    [MaxLength(20, ErrorMessage = "Biển số tối đa 20 ký tự")]
    [Display(Name = "Biển số xe")]
    public string PlateNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn dòng xe")]
    [Display(Name = "Dòng xe")]
    public Guid ModelId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chi nhánh")]
    [Display(Name = "Chi nhánh")]
    public Guid BranchId { get; set; }

    [MaxLength(50, ErrorMessage = "Màu sắc tối đa 50 ký tự")]
    [Display(Name = "Màu sắc")]
    public string? Color { get; set; }

    [Required(ErrorMessage = "Năm sản xuất là bắt buộc")]
    [Range(1900, 2100, ErrorMessage = "Năm sản xuất không hợp lệ")]
    [Display(Name = "Năm sản xuất")]
    public int ManufactureYear { get; set; } = DateTime.Now.Year;

    [Range(0, double.MaxValue, ErrorMessage = "Số km không hợp lệ")]
    [Display(Name = "Số km hiện tại")]
    public decimal CurrentOdoKm { get; set; } = 0;
}
