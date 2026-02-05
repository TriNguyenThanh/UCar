using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ViewModels.Contract;

/// <summary>
/// ViewModel cập nhật hợp đồng
/// </summary>
public class ContractEditViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public RentalContractStatus CurrentStatus { get; set; }

    // ===== Khách hàng (readonly nếu đã ký) =====
    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    // ===== Xe (readonly nếu đã ký) =====
    [Required(ErrorMessage = "Vui lòng chọn xe")]
    public Guid VehicleId { get; set; }
    public string? VehiclePlateNo { get; set; }
    public string? VehicleModel { get; set; }

    // ===== Thời gian =====
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime PlannedStart { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    [Display(Name = "Ngày kết thúc")]
    public DateTime PlannedEnd { get; set; }

    // ===== Địa điểm =====
    [MaxLength(500)]
    [Display(Name = "Địa điểm nhận xe")]
    public string? PickupLocation { get; set; }
    
    [MaxLength(500)]
    [Display(Name = "Địa điểm trả xe")]
    public string? ReturnLocation { get; set; }

    // ===== Tài chính =====
    [Required]
    public Guid PriceId { get; set; }
    
    [Display(Name = "Đơn giá/ngày")]
    public decimal UnitPrice { get; set; }
    
    [Display(Name = "Số ngày thuê")]
    public int RentalDays { get; set; }
    
    [Display(Name = "Tiền thuê")]
    public decimal RentalAmount { get; set; }
    
    [Display(Name = "Phụ phí")]
    public decimal ExtraCharges { get; set; }
    
    [Display(Name = "Tiền cọc")]
    public decimal DepositAmount { get; set; }
    
    [Display(Name = "Tổng tiền")]
    public decimal TotalAmount { get; set; }

    // ===== Điều khoản =====
    [MaxLength(4000)]
    [Display(Name = "Điều khoản hợp đồng")]
    public string? Terms { get; set; }
    
    [MaxLength(1000)]
    [Display(Name = "Ghi chú")]
    public string? InternalNote { get; set; }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    // ===== Logic - Luồng mới =====
    /// <summary>Chỉ có thể sửa những field quan trọng khi chưa ký (Draft hoặc PendingSigning)</summary>
    public bool CanEditCriticalFields => CurrentStatus == RentalContractStatus.Draft || CurrentStatus == RentalContractStatus.PendingSigning;
}

/// <summary>
/// ViewModel ký hợp đồng (khách hàng)
/// </summary>
public class ContractSignViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Tóm tắt hợp đồng
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string? Terms { get; set; }
    
    // Xác nhận
    [Required(ErrorMessage = "Vui lòng đồng ý điều khoản")]
    public bool AgreeToTerms { get; set; }
}

/// <summary>
/// ViewModel xác nhận hợp đồng (nhân viên)
/// </summary>
public class ContractConfirmViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Tóm tắt
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    
    // Xác nhận
    public bool CustomerHasSigned { get; set; }
    public DateTime? CustomerSignedAt { get; set; }
    
    [MaxLength(500)]
    public string? ConfirmNote { get; set; }
}

/// <summary>
/// ViewModel hủy hợp đồng
/// </summary>
public class ContractCancelViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public RentalContractStatus CurrentStatus { get; set; }
    
    // Tóm tắt
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public decimal DepositAmount { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy")]
    [MaxLength(1000)]
    [Display(Name = "Lý do hủy")]
    public string CancellationReason { get; set; } = string.Empty;
    
    // Thông tin hoàn cọc (nếu đã cọc)
    public bool HasDeposit { get; set; }
    public decimal RefundAmount { get; set; }
}

/// <summary>
/// ViewModel gia hạn hợp đồng (5.3)
/// </summary>
public class ContractExtendViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Thông tin hiển thị
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public DateTime CurrentStartDate { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public decimal DailyRate { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc mới")]
    [Display(Name = "Ngày kết thúc mới")]
    public DateTime NewEndDate { get; set; }
    
    public int AdditionalDays { get; set; }
    public decimal AdditionalCost { get; set; }
    public decimal NewTotalAmount { get; set; }
    
    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? ExtendNote { get; set; }
}
