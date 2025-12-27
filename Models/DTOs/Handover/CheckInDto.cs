using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO xác nhận nhận xe (Check-in)
/// </summary>
public class CheckInDto
{
    [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số km ODO")]
    [Range(0, 9999999, ErrorMessage = "Số km không hợp lệ")]
    public decimal OdoKmIn { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mức nhiên liệu")]
    [Range(0, 100, ErrorMessage = "Mức nhiên liệu phải từ 0-100%")]
    public decimal FuelLevelIn { get; set; }

    [MaxLength(1000)]
    public string? ExteriorCondition { get; set; }

    [MaxLength(1000)]
    public string? InteriorCondition { get; set; }

    [MaxLength(1000)]
    public string? DamagesFound { get; set; }

    /// <summary>Danh sách đường dẫn ảnh</summary>
    [MaxLength(2000)]
    public string? VehicleConditionImages { get; set; }

    /// <summary>Cần vệ sinh</summary>
    public bool NeedsCleaning { get; set; }

    /// <summary>Cần bảo dưỡng</summary>
    public bool NeedsMaintenance { get; set; }

    /// <summary>Phụ kiện đã trả</summary>
    public List<AccessoryReturnDto> AccessoriesReturned { get; set; } = new();

    /// <summary>Phụ phí phát sinh</summary>
    public List<ChargeDto> AdditionalCharges { get; set; } = new();

    [MaxLength(1000)]
    public string? Note { get; set; }
}

/// <summary>
/// DTO hiển thị form CheckIn
/// </summary>
public class CheckInFormDto
{
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Vehicle
    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    
    // Handover info (from CheckOut)
    public decimal OdoKmOut { get; set; }
    public decimal FuelLevelOut { get; set; }
    public DateTime HandedAt { get; set; }
    public string? PreExistingDamages { get; set; }
    
    // Schedule
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int PlannedDays => (int)Math.Ceiling((PlannedEnd - PlannedStart).TotalDays);
    
    // Overdue calculation
    public bool IsOverdue => DateTime.UtcNow > PlannedEnd;
    public double OvertimeHours => IsOverdue ? (DateTime.UtcNow - PlannedEnd).TotalHours : 0;
    
    // Accessories handed over
    public List<AccessoryReturnDto> HandedAccessories { get; set; } = new();
    
    // Branch
    public string BranchName { get; set; } = string.Empty;
}

/// <summary>
/// DTO phụ kiện trả lại
/// </summary>
public class AccessoryReturnDto
{
    public string AccessoryName { get; set; } = string.Empty;
    public int QuantityOut { get; set; }
    public int QuantityIn { get; set; }
    public bool IsReturnedOk { get; set; } = true;
    public string? DamageNote { get; set; }
    public decimal EstimatedValue { get; set; }
}

/// <summary>
/// DTO phụ phí phát sinh
/// </summary>
public class ChargeDto
{
    public ChargeType ChargeType { get; set; }
    public string ChargeTypeName => GetChargeTypeName(ChargeType);
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    
    private static string GetChargeTypeName(ChargeType type) => type switch
    {
        ChargeType.LateFee => "Phí trễ hạn",
        ChargeType.CleaningFee => "Phí vệ sinh",
        ChargeType.DamageFee => "Phí hư hỏng",
        ChargeType.FuelShortage => "Phí nhiên liệu thiếu",
        ChargeType.AccessoryLoss => "Phí mất phụ kiện",
        ChargeType.OvertimeFee => "Phí quá giờ",
        ChargeType.TollFee => "Phí cầu đường",
        _ => type.ToString()
    };
}
