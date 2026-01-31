using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel hiển thị danh sách chính sách phụ phí
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class SurchargePolicyListViewModel
{
    public Guid SurchargePolicyId { get; set; }
    
    [Display(Name = "Tên chính sách")]
    public string PolicyName { get; set; } = string.Empty;
    
    [Display(Name = "Loại phụ phí")]
    public SurchargeType Type { get; set; }
    
    [Display(Name = "Loại xe")]
    public string? VehicleTypeName { get; set; }
    
    public Guid? VehicleTypeId { get; set; }
    
    [Display(Name = "Phương thức tính")]
    public SurchargeCalculationType CalculationType { get; set; }
    
    [Display(Name = "Giá trị")]
    public decimal Value { get; set; }
    
    [Display(Name = "Tối thiểu")]
    public decimal? MinimumAmount { get; set; }
    
    [Display(Name = "Tối đa")]
    public decimal? MaximumAmount { get; set; }
    
    [Display(Name = "Mức ưu tiên")]
    public int Priority { get; set; }
    
    [Display(Name = "Hiệu lực từ")]
    public DateTime ValidFrom { get; set; }
    
    [Display(Name = "Hiệu lực đến")]
    public DateTime? ValidTo { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
}

/// <summary>
/// ViewModel lọc chính sách phụ phí
/// </summary>
public class SurchargePolicySearchViewModel
{
    [Display(Name = "Loại phụ phí")]
    public SurchargeType? Type { get; set; }
    
    [Display(Name = "Loại xe")]
    public Guid? VehicleTypeId { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool? IsActive { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
