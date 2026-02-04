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
    [Display(Name = "Chi nhánh")]
    public Guid? BranchId { get; set; }
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
    public Guid BranchId { get; set; } = Guid.Empty;
}

public class BookingCreateVM
{
    [Required]
    public Guid VehicleId { get; set; }
    
    // Display Info
    public string ModelName { get; set; } = string.Empty;
    public string PlateNo { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public Guid BranchId { get; set; } = Guid.Empty;

    [Required]
    public DateTime StartAt { get; set; }
    
    [Required]
    public DateTime EndAt { get; set; }
    
    // === PRICING DETAILS (Module 3) ===
    public int TotalDays { get; set; }
    public int NormalDays { get; set; }
    public int PeakDays { get; set; }
    
    public decimal BaseDailyPrice { get; set; }
    public decimal NormalDaysAmount { get; set; }
    public decimal PeakDaysAmount { get; set; }
    public decimal MonthlyAmount { get; set; }
    public bool IsMonthlyRate { get; set; }
    
    public decimal RentalAmount { get; set; }  // Subtotal before deposits
    
    // === DEPOSIT DETAILS ===
    public decimal ResponsibilityDeposit { get; set; } // Cọc trách nhiệm
    public decimal RentalDeposit { get; set; }  // Cọc thuê xe
    public decimal TotalDeposit { get; set; }
    
    // === TOTAL ===
    public decimal EstimatedPrice { get; set; }  // For backward compatibility
    public decimal TotalAmount { get; set; }  // RentalAmount + TotalDeposit
    
    // Customer Info (Pre-filled)
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Nơi nhận xe")]
    public string PickUpLocation { get; set; } = string.Empty;
    
    [Display(Name = "Nơi trả xe")]
    public string? DropOffLocation { get; set; }

    [Display(Name = "Số điện thoại")]
    public string CustomerPhone { get; set; } = string.Empty;
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
    
    // ===== Chi tiết giá dự kiến =====
    /// <summary>Số ngày thường</summary>
    public int NormalDays { get; set; }
    
    /// <summary>Số ngày lễ</summary>
    public int PeakDays { get; set; }
    
    /// <summary>Đơn giá ngày thường</summary>
    public decimal DailyPrice { get; set; }
    
    /// <summary>Hệ số ngày lễ</summary>
    public decimal PeakMultiplier { get; set; }
    
    /// <summary>Tiền thuê ngày thường</summary>
    public decimal NormalDaysAmount { get; set; }
    
    /// <summary>Tiền thuê ngày lễ</summary>
    public decimal PeakDaysAmount { get; set; }
    
    /// <summary>Tiền cọc trách nhiệm</summary>
    public decimal ResponsibilityDeposit { get; set; }
    
    /// <summary>Tiền cọc thuê xe (50%)</summary>
    public decimal RentalDeposit { get; set; }
    
    /// <summary>Tổng tiền cọc</summary>
    public decimal TotalDeposit => ResponsibilityDeposit + RentalDeposit;
    
    /// <summary>Tổng tiền cần thanh toán khi nhận xe = Tiền thuê + Cọc</summary>
    public decimal TotalPickupAmount => EstimatedTotal + TotalDeposit;
    
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // Permissions
    public bool CanCancel { get; set; }
    public bool CanApprove { get; set; } // Staff only
    public bool CanReject { get; set; } // Staff only
    
    // ===== Contract Info =====
    /// <summary>
    /// Có hợp đồng active (không bị hủy) liên kết với booking này
    /// </summary>
    public bool HasActiveContract { get; set; }
    
    /// <summary>
    /// ID của hợp đồng (nếu có)
    /// </summary>
    public Guid? ContractId { get; set; }
    
    /// <summary>
    /// Mã hợp đồng (ví dụ: HD-001234)
    /// </summary>
    public string? ContractCode { get; set; }
    
    /// <summary>
    /// Trạng thái hợp đồng
    /// </summary>
    public RentalContractStatus? ContractStatus { get; set; }
    
    /// <summary>
    /// Text hiển thị trạng thái hợp đồng - Luồng mới
    /// </summary>
    public string ContractStatusDisplay => ContractStatus switch
    {
        RentalContractStatus.Draft => "Bản nháp",
        RentalContractStatus.PendingSigning => "Chờ ký",
        RentalContractStatus.Active => "Đang hoạt động",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.PendingSettlement => "Chờ quyết toán",
        RentalContractStatus.Completed => "Hoàn tất",
        RentalContractStatus.Disputed => "Tranh chấp",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => ""
    };
    
    /// <summary>
    /// CSS class cho chip trạng thái hợp đồng - Luồng mới
    /// </summary>
    public string ContractStatusClass => ContractStatus switch
    {
        RentalContractStatus.Draft => "chip grey lighten-1",
        RentalContractStatus.PendingSigning => "chip orange white-text",
        RentalContractStatus.Active => "chip blue white-text",
        RentalContractStatus.InProgress => "chip teal white-text",
        RentalContractStatus.PendingSettlement => "chip deep-orange white-text",
        RentalContractStatus.Completed => "chip green darken-2 white-text",
        RentalContractStatus.Disputed => "chip red white-text",
        RentalContractStatus.Cancelled => "chip grey white-text",
        _ => "chip"
    };
    
    /// <summary>
    /// Icon cho trạng thái hợp đồng - Luồng mới
    /// </summary>
    public string ContractStatusIcon => ContractStatus switch
    {
        RentalContractStatus.Draft => "edit",
        RentalContractStatus.PendingSigning => "schedule",
        RentalContractStatus.Active => "play_circle",
        RentalContractStatus.InProgress => "directions_car",
        RentalContractStatus.PendingSettlement => "receipt_long",
        RentalContractStatus.Completed => "verified",
        RentalContractStatus.Disputed => "report",
        RentalContractStatus.Cancelled => "cancel",
        _ => "description"
    };
}

public class BookingActionVM
{
    [Required]
    public Guid BookingId { get; set; }
    
    public string? Reason { get; set; } // For Reject/Cancel
}
