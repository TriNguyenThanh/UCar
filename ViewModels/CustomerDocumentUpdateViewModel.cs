using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UCar.Models.Enums;

namespace UCar.ViewModels;

/// <summary>
/// ViewModel for customer document update
/// Cho phép customer cập nhật giấy tờ tùy thân
/// </summary>
public class CustomerDocumentUpdateViewModel
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? DocId { get; set; }

    [Required(ErrorMessage = "Loại giấy tờ là bắt buộc")]
    [Display(Name = "Loại giấy tờ")]
    public CustomerDocumentType DocType { get; set; }

    [Required(ErrorMessage = "Số giấy tờ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Số giấy tờ không được vượt quá 100 ký tự")]
    [Display(Name = "Số CCCD/GPLX/Passport")]
    public string DocNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp")]
    public DateTime? IssuedDate { get; set; }

    [StringLength(200, ErrorMessage = "Nơi cấp không được vượt quá 200 ký tự")]
    [Display(Name = "Nơi cấp")]
    public string? IssuedPlace { get; set; }

    [Display(Name = "Ảnh mặt trước")]
    public IFormFile? DocumentImageFront { get; set; }

    [Display(Name = "Ảnh mặt sau")]
    public IFormFile? DocumentImageBack { get; set; }

    // Existing images for display
    public string? ExistingImageFrontUrl { get; set; }
    public string? ExistingImageBackUrl { get; set; }

    // Verification status (readonly, for display)
    [Display(Name = "Đã xác minh")]
    public bool IsVerified { get; set; }

    // Customer name for display
    public string? CustomerFullName { get; set; }
}
