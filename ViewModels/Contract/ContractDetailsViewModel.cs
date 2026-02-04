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
    
    // ===== Thanh toán (Luồng mới) =====
    /// <summary>Đã có biên bản giao xe</summary>
    public bool HasHandoverRecord { get; set; }
    /// <summary>Đã thanh toán hóa đơn giao xe</summary>
    public bool HasPaidDeliveryInvoice { get; set; }

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

    // ===== Trạng thái logic - Luồng mới =====
    public bool CanEdit => Status == RentalContractStatus.Draft || Status == RentalContractStatus.PendingSigning;
    public bool CanSign => Status == RentalContractStatus.PendingSigning && !CustomerSigned; // Ký giấy tại quầy
    /// <summary>
    /// Có thể thanh toán: Đang ở PendingSigning và chưa thanh toán hóa đơn giao xe
    /// </summary>
    public bool CanPayPickup => Status == RentalContractStatus.PendingSigning && HasHandoverRecord && !HasPaidDeliveryInvoice;
    /// <summary>
    /// Có thể xác nhận: Không dùng nữa - xác nhận được tích hợp vào luồng thanh toán
    /// </summary>
    public bool CanConfirm => false; // Luồng mới: Xác nhận tự động khi thanh toán xong
    public bool CanCancel => Status == RentalContractStatus.Draft || Status == RentalContractStatus.PendingSigning;
    public bool CanPrint => Status != RentalContractStatus.Draft;
    /// <summary>
    /// Có thể lập biên bản: Status là Draft (chưa lập biên bản)
    /// </summary>
    public bool CanCreateHandover => Status == RentalContractStatus.Draft && !HasHandoverRecord;
    /// <summary>
    /// Có thể bàn giao: Chỉ để tương thích ngược, ưu tiên dùng CanCreateHandover hoặc CanPayPickup
    /// </summary>
    public bool CanHandover => Status == RentalContractStatus.Draft || (Status == RentalContractStatus.PendingSigning && !HasPaidDeliveryInvoice);
    
    private string GetStatusDisplay() => Status switch
    {
        RentalContractStatus.Draft => "Bản nháp",
        RentalContractStatus.PendingSigning => "Chờ ký",
        RentalContractStatus.Active => "Đang hoạt động",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.PendingSettlement => "Chờ quyết toán",
        RentalContractStatus.Completed => "Hoàn tất",
        RentalContractStatus.Disputed => "Tranh chấp",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };
    
    private string GetStatusClass() => Status switch
    {
        RentalContractStatus.Draft => "chip",
        RentalContractStatus.PendingSigning => "chip chip-warning",
        RentalContractStatus.Active => "chip chip-primary",
        RentalContractStatus.InProgress => "chip chip-success",
        RentalContractStatus.PendingSettlement => "chip chip-warning",
        RentalContractStatus.Completed => "chip chip-success",
        RentalContractStatus.Disputed => "chip chip-error",
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
