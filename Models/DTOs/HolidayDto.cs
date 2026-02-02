using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs;

/// <summary>
/// DTO for creating/updating holidays
/// </summary>
public class HolidayDto
{
    [Required(ErrorMessage = "Tên ngày lễ là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên ngày lễ không được vượt quá 200 ký tự")]
    public string HolidayName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
    
    [Required(ErrorMessage = "Năm là bắt buộc")]
    [Range(2020, 2100, ErrorMessage = "Năm phải từ 2020 đến 2100")]
    public int Year { get; set; }
    
    public bool IsActive { get; set; } = true;
}
