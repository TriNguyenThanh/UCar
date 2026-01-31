using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels.Holiday;

public class HolidayListViewModel
{
    public Guid HolidayId { get; set; }
    
    [Display(Name = "Tên ngày lễ")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Ngày (Dương lịch)")]
    public DateTime? Date { get; set; }
    
    [Display(Name = "Ngày (Âm lịch)")]
    public string? LunarDate { get; set; }
    
    [Display(Name = "Kỳ nghỉ")]
    public string? DateRange { get; set; }
    
    [Display(Name = "Lặp lại")]
    public bool IsRecurring { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
}
