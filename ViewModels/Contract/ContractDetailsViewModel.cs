using UCar.Models.Enums;

namespace UCar.ViewModels.Contract;

/// <summary>
/// ViewModel chi tiết hợp đồng
/// </summary>
public class ContractDetailsViewModel
{
    // ===== Thông tin hợp đồng =====
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public RentalContractStatus Status { get; set; }
    public string StatusDisplay => GetStatusDisplay();
    public string StatusClass => GetStatusClass();

    // ===== Khách hàng =====
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerIdNumber { get; set; }

    // ===== Xe =====
    public Guid VehicleId { get; set; }
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public string? VehicleColor { get; set; }
    public int VehicleYear { get; set; }
    public string BranchName { get; set; } = string.Empty;

    // ===== Thời gian =====
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int RentalDays { get; set; }
    public int? ActualRentalDays { get; set; }

    // ===== Địa điểm =====
    public string? PickupLocation { get; set; }
    public string? ReturnLocation { get; set; }

    // ===== Tài chính =====
    public decimal UnitPrice { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal ExtraCharges { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Chi tiết phụ phí
    public List<ChargeDetailViewModel> Charges { get; set; } = new();

    // ===== Điều khoản =====
    public string? Terms { get; set; }
    public string? InternalNote { get; set; }

    // ===== Ký / Xác nhận =====
    public bool CustomerSigned { get; set; }
    public DateTime? CustomerSignedAt { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // ===== Hủy =====
    public string? CancellationReason { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }

    // ===== Liên kết =====
    public Guid? BookingId { get; set; }
    public string? BookingCode { get; set; }
    public Guid? HandoverId { get; set; }
    public DateTime? HandedAt { get; set; }
    public Guid? ReturnId { get; set; }
    public DateTime? ReturnedAt { get; set; }
    
    // Vi phạm
    public List<ViolationSummaryViewModel> Violations { get; set; } = new();

    // ===== Audit =====
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }

    // ===== Trạng thái logic =====
    public bool CanEdit => Status == RentalContractStatus.Draft || Status == RentalContractStatus.Pending;
    public bool CanSign => Status == RentalContractStatus.Pending && !CustomerSigned;
    public bool CanConfirm => Status == RentalContractStatus.Signed || (Status == RentalContractStatus.Pending && CustomerSigned);
    public bool CanCancel => Status == RentalContractStatus.Draft || Status == RentalContractStatus.Pending || Status == RentalContractStatus.Signed;
    public bool CanPrint => Status != RentalContractStatus.Draft;
    public bool CanHandover => Status == RentalContractStatus.Active || Status == RentalContractStatus.AwaitingDelivery;
    
    private string GetStatusDisplay() => Status switch
    {
        RentalContractStatus.Draft => "Bản nháp",
        RentalContractStatus.Pending => "Chờ ký",
        RentalContractStatus.Signed => "Đã ký",
        RentalContractStatus.Active => "Đang hoạt động",
        RentalContractStatus.AwaitingDelivery => "Chờ giao xe",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.AwaitingReturn => "Chờ trả xe",
        RentalContractStatus.PendingSettlement => "Chờ quyết toán",
        RentalContractStatus.Completed => "Hoàn tất",
        RentalContractStatus.Violation => "Vi phạm",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };
    
    private string GetStatusClass() => Status switch
    {
        RentalContractStatus.Draft => "chip",
        RentalContractStatus.Pending => "chip chip-warning",
        RentalContractStatus.Signed => "chip chip-info",
        RentalContractStatus.Active => "chip chip-primary",
        RentalContractStatus.AwaitingDelivery => "chip chip-info",
        RentalContractStatus.InProgress => "chip chip-success",
        RentalContractStatus.AwaitingReturn => "chip chip-warning",
        RentalContractStatus.PendingSettlement => "chip chip-warning",
        RentalContractStatus.Completed => "chip chip-success",
        RentalContractStatus.Violation => "chip chip-error",
        RentalContractStatus.Cancelled => "chip chip-error",
        _ => "chip"
    };
}

public class ChargeDetailViewModel
{
    public Guid ChargeId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ViolationSummaryViewModel
{
    public Guid ViolationId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
