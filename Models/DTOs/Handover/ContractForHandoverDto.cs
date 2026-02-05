using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO hiển thị danh sách hợp đồng chờ giao xe
/// </summary>
public class ContractForHandoverListDto
{
    public Guid ContractId { get; set; }
    public string ContractCode => $"HD-{ContractId.ToString()[..8].ToUpper()}";
    
    // Customer info
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Vehicle info
    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty; // Make + Model
    public string VehicleTypeName { get; set; } = string.Empty;
    
    // Branch info
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    
    // Schedule
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    
    // Status
    public RentalContractStatus Status { get; set; }
    public string StatusDisplayName => GetStatusDisplayName(Status);
    
    // Flags
    public bool HasHandoverRecord { get; set; }
    public bool HasReturnRecord { get; set; }
    public bool IsOverdue => PlannedEnd < DateTime.UtcNow && !HasReturnRecord;
    
    private static string GetStatusDisplayName(RentalContractStatus status) => status switch
    {
        RentalContractStatus.Draft => "Chờ lập biên bản",
        RentalContractStatus.PendingSigning => "Chờ thanh toán",
        RentalContractStatus.Active => "Đang thuê",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.PendingSettlement => "Chờ quyết toán",
        RentalContractStatus.Completed => "Hoàn tất",
        RentalContractStatus.Disputed => "Tranh chấp",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };
}

/// <summary>
/// DTO filter danh sách hợp đồng giao nhận
/// </summary>
public class HandoverFilterDto
{
    public string? SearchTerm { get; set; }
    public Guid? BranchId { get; set; }
    public RentalContractStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsOverdue { get; set; }
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
