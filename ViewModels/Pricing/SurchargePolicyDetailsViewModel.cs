using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel hiển thị chi tiết chính sách phụ phí
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class SurchargePolicyDetailsViewModel
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
    
    [Display(Name = "Số tiền tối thiểu")]
    public decimal? MinimumAmount { get; set; }
    
    [Display(Name = "Số tiền tối đa")]
    public decimal? MaximumAmount { get; set; }
    
    [Display(Name = "Điều kiện áp dụng")]
    public string? ApplicableCondition { get; set; }
    
    [Display(Name = "Mức độ ưu tiên")]
    public int Priority { get; set; }
    
    [Display(Name = "Hiệu lực từ")]
    public DateTime ValidFrom { get; set; }
    
    [Display(Name = "Hiệu lực đến")]
    public DateTime? ValidTo { get; set; }
    
    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
    
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
    
    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; }
    
    [Display(Name = "Ngày cập nhật")]
    public DateTime? UpdatedAt { get; set; }
}
