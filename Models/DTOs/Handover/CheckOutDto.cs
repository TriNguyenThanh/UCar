using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO xác nhận giao xe (Check-out)
/// </summary>
public class CheckOutDto
{
    [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số km ODO")]
    [Range(0, 9999999, ErrorMessage = "Số km không hợp lệ")]
    public decimal OdoKmOut { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mức nhiên liệu")]
    [Range(0, 100, ErrorMessage = "Mức nhiên liệu phải từ 0-100%")]
    public decimal FuelLevelOut { get; set; }

    [MaxLength(1000)]
    public string? ExteriorCondition { get; set; }

    [MaxLength(1000)]
    public string? InteriorCondition { get; set; }

    [MaxLength(1000)]
    public string? PreExistingDamages { get; set; }

    /// <summary>Danh sách đường dẫn ảnh (JSON array)</summary>
    [MaxLength(2000)]
    public string? VehicleConditionImages { get; set; }

    /// <summary>Phụ kiện bàn giao</summary>
    public List<AccessoryDto> Accessories { get; set; } = new();

    /// <summary>Khách hàng đã xác nhận</summary>
    public bool CustomerConfirmed { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

/// <summary>
/// DTO hiển thị form CheckOut
/// </summary>
public class CheckOutFormDto
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerIdNumber { get; set; } = string.Empty;
    
    // Vehicle
    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public decimal CurrentOdoKm { get; set; }
    
    // Schedule
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int RentalDays => (int)Math.Ceiling((PlannedEnd - PlannedStart).TotalDays);
    
    // Payment info (stub - P7 chưa hoàn thiện)
    public decimal DepositAmount { get; set; }
    public bool DepositPaid { get; set; } = true; // TODO: Lấy từ P7
    public decimal RentalAmount { get; set; }
    
    // Branch
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    
    // Default accessories
    public List<AccessoryDto> DefaultAccessories { get; set; } = new();
}

/// <summary>
/// DTO phụ kiện
/// </summary>
public class AccessoryDto
{
    public string AccessoryName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal EstimatedValue { get; set; }
    public bool IsChecked { get; set; } = true;
    public string? Note { get; set; }
}
