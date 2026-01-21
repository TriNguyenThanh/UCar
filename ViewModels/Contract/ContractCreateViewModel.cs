using System.ComponentModel.DataAnnotations;

namespace UCar.ViewModels.Contract;

/// <summary>
/// ViewModel tạo hợp đồng
/// </summary>
public class ContractCreateViewModel
{
    // ===== Nguồn tạo =====
    
    /// <summary>Tạo từ booking (nếu có)</summary>
    public Guid? BookingId { get; set; }
    
    /// <summary>True = Walk-in (tạo tại quầy)</summary>
    public bool IsWalkIn { get; set; }

    // ===== Khách hàng =====
    
    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    [Display(Name = "Khách hàng")]
    public Guid CustomerId { get; set; }
    
    // Thông tin hiển thị
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerIdNumber { get; set; }

    // ===== Xe =====
    
    [Required(ErrorMessage = "Vui lòng chọn xe")]
    [Display(Name = "Xe cho thuê")]
    public Guid VehicleId { get; set; }
    
    // Thông tin hiển thị
    public string? VehiclePlateNo { get; set; }
    public string? VehicleModel { get; set; }

    // ===== Thời gian =====
    
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [Display(Name = "Ngày bắt đầu")]
    [DataType(DataType.DateTime)]
    public DateTime PlannedStart { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    [Display(Name = "Ngày kết thúc")]
    [DataType(DataType.DateTime)]
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
    [Display(Name = "Bảng giá")]
    public Guid PriceId { get; set; }
    
    [Display(Name = "Đơn giá/ngày")]
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0")]
    public decimal UnitPrice { get; set; }
    
    [Display(Name = "Số ngày thuê")]
    public int RentalDays { get; set; }
    
    [Display(Name = "Tiền thuê")]
    public decimal RentalAmount { get; set; }
    
    [Display(Name = "Phụ phí")]
    [Range(0, double.MaxValue)]
    public decimal ExtraCharges { get; set; }
    
    [Display(Name = "Tiền cọc")]
    [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc phải >= 0")]
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

    // ===== Lưu nháp hay submit =====
    
    /// <summary>True = lưu bản nháp, False = tạo và chờ ký</summary>
    public bool SaveAsDraft { get; set; }
}

/// <summary>
/// Thông tin booking để tạo hợp đồng
/// </summary>
public class BookingForContractViewModel
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    public Guid VehicleId { get; set; }
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal EstimatedTotal { get; set; }
    
    public Guid PriceId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DepositSuggest { get; set; }
}

/// <summary>
/// Dropdown options cho form tạo HĐ
/// </summary>
public class ContractCreateOptionsViewModel
{
    public List<CustomerOption> Customers { get; set; } = new();
    public List<VehicleOption> Vehicles { get; set; } = new();
    public List<PriceOption> Prices { get; set; } = new();
    public string DefaultTerms { get; set; } = string.Empty;
}

public class CustomerOption
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? IdNumber { get; set; }
    public bool IsBlacklisted { get; set; }
}

public class VehicleOption
{
    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public Guid VehicleTypeId { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public class PriceOption
{
    public Guid PriceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid VehicleTypeId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DepositSuggest { get; set; }
}
