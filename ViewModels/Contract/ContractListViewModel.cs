using UCar.Models.Enums;

namespace UCar.ViewModels.Contract;

/// <summary>
/// ViewModel cho danh sách hợp đồng
/// </summary>
public class ContractListViewModel
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Khách hàng
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Xe
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    
    // Thời gian
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int RentalDays { get; set; }
    
    // Tài chính
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    
    // Trạng thái
    public RentalContractStatus Status { get; set; }
    public string StatusDisplay => GetStatusDisplay();
    public string StatusClass => GetStatusClass();
    
    // Handover Integration
    /// <summary>
    /// Hợp đồng sẵn sàng giao xe (đã ký và booking chưa bị hủy)
    /// </summary>
    public bool IsReadyForHandover { get; set; }
    
    /// <summary>
    /// Booking liên kết đã bị hủy
    /// </summary>
    public bool IsBookingCancelled { get; set; }
    
    /// <summary>
    /// Icon và class hiển thị cho trạng thái giao xe
    /// </summary>
    public string HandoverStatusIcon => IsReadyForHandover ? "check_circle" : (IsBookingCancelled ? "cancel" : "schedule");
    public string HandoverStatusClass => IsReadyForHandover ? "green-text" : (IsBookingCancelled ? "red-text" : "orange-text");
    public string HandoverStatusText => IsReadyForHandover ? "Sẵn sàng" : (IsBookingCancelled ? "Đã hủy" : "Chờ ký");
    
    // Booking liên quan
    public Guid? BookingId { get; set; }
    public string? BookingCode { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    
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

/// <summary>
/// Filter và phân trang cho danh sách hợp đồng
/// </summary>
public class ContractSearchViewModel
{
    public string? Keyword { get; set; }
    public RentalContractStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? CustomerId { get; set; }
    
    // Sorting
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDesc { get; set; } = true;
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Kết quả phân trang
/// </summary>
public class ContractListResultViewModel
{
    public List<ContractListViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    
    // Filter state
    public ContractSearchViewModel Filter { get; set; } = new();
    
    // Dropdown data
    public List<StatusOption> StatusOptions { get; set; } = new();
    public List<BranchOption> BranchOptions { get; set; } = new();
}

public class StatusOption
{
    public RentalContractStatus Value { get; set; }
    public string Display { get; set; } = string.Empty;
}

public class BranchOption
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
}
