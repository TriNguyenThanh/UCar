using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for pending document approval list
/// </summary>
public class PendingDocumentViewModel
{
    public Guid DocId { get; set; }
    public Guid CustomerId { get; set; }
    
    [Display(Name = "Khách hàng")]
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Loại giấy tờ")]
    public CustomerDocumentType DocType { get; set; }
    
    [Display(Name = "Số giấy tờ")]
    public string? DocNumber { get; set; }
    
    [Display(Name = "Ngày cấp")]
    public DateTime? IssuedDate { get; set; }
    
    [Display(Name = "Nơi cấp")]
    public string? IssuedPlace { get; set; }
    
    public string? ImageFrontUrl { get; set; }
    public string? ImageBackUrl { get; set; }
    
    [Display(Name = "Ngày đăng ký")]
    public DateTime CustomerCreatedAt { get; set; }
    
    // Helper for display
    public string DocTypeName => DocType switch
    {
        CustomerDocumentType.IdCard => "CCCD/CMND",
        CustomerDocumentType.License => "Giấy phép lái xe",
        CustomerDocumentType.Passport => "Hộ chiếu",
        _ => "Khác"
    };
}
