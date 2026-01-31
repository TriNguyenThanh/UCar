using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Pricing;

/// <summary>
/// ViewModel hiển thị chi tiết chính sách đặt cọc
/// Module 3: Quản lý bảng giá & chính sách
/// </summary>
public class DepositPolicyDetailsViewModel
{
    public Guid DepositPolicyId { get; set; }
    
    [Display(Name = "Tên chính sách")]
    public string PolicyName { get; set; } = string.Empty;
    
    [Display(Name = "Loại xe")]
    public string VehicleTypeName { get; set; } = string.Empty;
    
    public Guid VehicleTypeId { get; set; }
    
    [Display(Name = "Phương thức tính")]
    public DepositCalculationType CalculationType { get; set; }
    
    [Display(Name = "Giá trị")]
    public decimal Value { get; set; }
    
    [Display(Name = "Số tiền tối thiểu")]
    public decimal MinimumAmount { get; set; }
    
    [Display(Name = "Số tiền tối đa")]
    public decimal MaximumAmount { get; set; }
    
    [Display(Name = "Điều kiện hoàn cọc 100%")]
    public string? FullRefundCondition { get; set; }
    
    [Display(Name = "Điều kiện hoàn cọc 50%")]
    public string? PartialRefundCondition { get; set; }
    
    [Display(Name = "Điều kiện không hoàn cọc")]
    public string? NoRefundCondition { get; set; }
    
    [Display(Name = "Số ngày xử lý hoàn cọc")]
    public int RefundProcessingDays { get; set; }
    
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
