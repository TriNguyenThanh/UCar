namespace UCar.ViewModels.Contract;

using UCar.Models.Enums;

/// <summary>
/// ViewModel cho in/xuất hợp đồng
/// </summary>
public class ContractPrintViewModel
{
    // ===== Header =====
    public string ContractCode { get; set; } = string.Empty;
    public DateTime PrintDate { get; set; } = DateTime.Now;
    public string CompanyName { get; set; } = "CÔNG TY TNHH CHO THUÊ XE UCAR";
    public string CompanyAddress { get; set; } = "123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh";
    public string CompanyPhone { get; set; } = "1900 1234";
    public string CompanyEmail { get; set; } = "contact@ucar.vn";
    public string CompanyTaxCode { get; set; } = "0123456789";
    public string Status { get; set; } = string.Empty;

    // ===== Bên cho thuê (Party A) =====
    public string PartyAName { get; set; } = string.Empty;
    public string PartyARepresentative { get; set; } = string.Empty;
    public string PartyAPosition { get; set; } = string.Empty;
    public string PartyAPhone { get; set; } = string.Empty;

    // ===== Bên thuê (Party B) - Khách hàng =====
    public string CustomerName { get; set; } = string.Empty;
    public DateTime? CustomerDob { get; set; }
    public string? CustomerIdNumber { get; set; }
    public string? CustomerIdIssueDate { get; set; }
    public string? CustomerIdIssuePlace { get; set; }
    public string? CustomerAddress { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerDriverLicense { get; set; }

    // ===== Thông tin xe =====
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public string? VehicleColor { get; set; }
    public string? VehicleVin { get; set; }
    public string? VehicleRegistrationNo { get; set; }
    public string VehicleInfo => $"{VehicleBrand} {VehicleModel} ({VehicleYear})";
    public string LicensePlate => VehiclePlateNo;

    // ===== Thời gian & địa điểm =====
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int RentalDays { get; set; }
    public string? PickupLocation { get; set; }
    public string? ReturnLocation { get; set; }

    // ===== Chi phí =====
    public decimal UnitPrice { get; set; }
    public decimal DailyRate => UnitPrice;
    public string UnitPriceText { get; set; } = string.Empty;
    public decimal RentalAmount { get; set; }
    public decimal ExtraCharges { get; set; }
    public decimal DepositAmount { get; set; }
    public string DepositAmountText { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string TotalAmountText { get; set; } = string.Empty;

    // ===== Điều khoản =====
    public string Terms { get; set; } = string.Empty;

    // ===== Ký xác nhận =====
    public bool CustomerSigned { get; set; }
    public DateTime? CustomerSignedAt { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // ===== Footer =====
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
