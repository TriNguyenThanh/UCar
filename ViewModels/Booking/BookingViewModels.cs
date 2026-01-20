using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Booking;

public class BookingSearchCheckVM
{
    [Display(Name = "Ngày nhận xe")]
    [Required(ErrorMessage = "Vui lòng chọn ngày nhận xe")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Giờ nhận")]
    [Required(ErrorMessage = "Vui lòng chọn giờ nhận")]
    public TimeSpan? StartTime { get; set; }

    [Display(Name = "Ngày trả xe")]
    [Required(ErrorMessage = "Vui lòng chọn ngày trả xe")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Giờ trả")]
    [Required(ErrorMessage = "Vui lòng chọn giờ trả")]
    public TimeSpan? EndTime { get; set; }
    
    // Filter properties
    [Display(Name = "Loại xe")]
    public Guid? VehicleTypeId { get; set; }
    
    [Display(Name = "Hãng xe")]
    public string? Make { get; set; }
    
    [Display(Name = "Số ghế")]
    public int? Seats { get; set; }
}

public class VehicleSearchResultVM
{
    public Guid VehicleId { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public string PlateNo { get; set; } = string.Empty; // Masked if needed?
    public string Color { get; set; } = string.Empty;
    public int Year { get; set; }
    public string ImageUrl { get; set; } = "https://placehold.co/600x400?text=Car"; // Placeholder
    public decimal DailyPrice { get; set; }
    public decimal EstimatedTotal { get; set; }
    public string Transmission { get; set; } = string.Empty; // "Tự động"
    public int Seats { get; set; }
    public bool IsAvailable { get; set; }
}

public class BookingCreateVM
{
    [Required]
    public Guid VehicleId { get; set; }
    
    // Display Info
    public string ModelName { get; set; } = string.Empty;
    public string PlateNo { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    public DateTime StartAt { get; set; }
    
    [Required]
    public DateTime EndAt { get; set; }
    
    public decimal EstimatedPrice { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Customer Info (Pre-filled)
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;
    
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public class BookingListVM
{
    public Guid BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty; // Model + Plate
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookingDetailVM
{
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    
    public Guid VehicleTypeId { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    
    public Guid? AssignedVehicleId { get; set; }
    public string? AssignedVehicleName { get; set; }
    public string? AssignedVehiclePlate { get; set; }
    
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalDays { get; set; }
    
    public decimal EstimatedTotal { get; set; }
    
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // Permissions
    public bool CanCancel { get; set; }
    public bool CanApprove { get; set; } // Staff only
    public bool CanReject { get; set; } // Staff only
}

public class BookingActionVM
{
    [Required]
    public Guid BookingId { get; set; }
    
    public string? Reason { get; set; } // For Reject/Cancel
}
