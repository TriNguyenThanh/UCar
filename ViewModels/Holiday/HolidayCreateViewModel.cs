using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels.Holiday;

public class HolidayCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên ngày lễ")]
    [MaxLength(200, ErrorMessage = "Tên ngày lễ không được vượt quá 200 ký tự")]
    [Display(Name = "Tên ngày lễ")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Ngày lễ (Dương lịch)")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }
    
    [MaxLength(10)]
    [Display(Name = "Ngày theo Âm lịch")]
    public string? LunarDate { get; set; }
    
    [Display(Name = "Ngày bắt đầu kỳ nghỉ")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }
    
    [Display(Name = "Ngày kết thúc kỳ nghỉ")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
    
    [Display(Name = "Lặp lại hàng năm")]
    public bool IsRecurring { get; set; } = true;
    
    [MaxLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
    
    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;
}
