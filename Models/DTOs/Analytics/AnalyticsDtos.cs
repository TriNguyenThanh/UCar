using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs.Analytics;

/// <summary>
/// DTO cho báo cáo doanh thu, lợi nhuận và chi phí (DFD 9.1)
/// Lưu ý: Đặt cọc KHÔNG tính vào doanh thu vì sẽ hoàn trả khách
/// </summary>
public class RevenueReportDto
{
    // ===== TỔNG QUAN DOANH THU =====
    public decimal TotalRevenue { get; set; }          // Tổng doanh thu (không bao gồm đặt cọc)
    public decimal TotalPaid { get; set; }             // Đã thu
    public decimal TotalDue { get; set; }              // Còn phải thu
    
    // ===== CƠ CẤU DOANH THU (không bao gồm đặt cọc) =====
    public decimal RentalRevenue { get; set; }         // Tiền thuê cơ bản
    public decimal NormalDaysRevenue { get; set; }     // Tiền thuê ngày thường
    public decimal PeakDaysRevenue { get; set; }       // Tiền thuê ngày cao điểm (lễ, Tết)
    public decimal SurchargeRevenue { get; set; }      // Phụ phí (overtime, vượt km, etc.)
    public decimal PenaltyRevenue { get; set; }        // Phí phạt (trễ hạn, hư hỏng, etc.)
    public decimal TaxRevenue { get; set; }            // Thuế
    public decimal DiscountAmount { get; set; }        // Giảm giá (số âm hoặc dương)
    
    // ===== ĐẶT CỌC (Riêng biệt - không phải doanh thu) =====
    public decimal TotalDeposits { get; set; }         // Tổng tiền cọc đã thu
    public decimal DepositsRefunded { get; set; }      // Cọc đã hoàn trả
    public decimal DepositsHeld { get; set; }          // Cọc đang giữ
    public decimal DepositsForfeited { get; set; }     // Cọc khấu trừ (tính vào doanh thu khi khấu trừ)
    
    // ===== THỐNG KÊ HỢP ĐỒNG =====
    public int TotalContracts { get; set; }
    public int CompletedContracts { get; set; }
    public int ActiveContracts { get; set; }
    
    public List<MonthlyRevenueDto> MonthlyData { get; set; } = new();
}

/// <summary>
/// DTO cho doanh thu theo tháng
/// </summary>
public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;      // "01/2026"
    public decimal Revenue { get; set; }
    public int Contracts { get; set; }
}

/// <summary>
/// DTO cho báo cáo tỷ lệ sử dụng xe (DFD 9.2)
/// </summary>
public class VehicleUtilizationReportDto
{
    public int TotalVehicles { get; set; }
    public int AvailableVehicles { get; set; }
    public int RentingVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public int ReservedVehicles { get; set; }
    public int OtherStatusVehicles { get; set; }
    
    public decimal UtilizationRate { get; set; }     // % xe đang thuê / tổng xe active
    public decimal AvailabilityRate { get; set; }    // % xe sẵn sàng / tổng xe active
    
    public List<VehiclePerformanceDto> TopVehicles { get; set; } = new();
}

/// <summary>
/// DTO cho hiệu suất xe
/// </summary>
public class VehiclePerformanceDto
{
    public Guid VehicleId { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TotalRentals { get; set; }            // Số lần thuê
    public decimal TotalRevenue { get; set; }        // Tổng doanh thu
    public int TotalDays { get; set; }               // Tổng ngày thuê
    public decimal AverageDailyRevenue { get; set; } // Doanh thu trung bình/ngày
}

/// <summary>
/// DTO cho báo cáo khách hàng (DFD 9.3)
/// </summary>
public class CustomerReportDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }          // Có booking/contract trong 6 tháng
    public int BlacklistedCustomers { get; set; }
    public int NewCustomers { get; set; }             // Khách mới trong tháng
    
    public decimal TotalRevenue { get; set; }         // Tổng doanh thu từ khách
    public decimal AverageCustomerValue { get; set; } // Giá trị trung bình/khách
    
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<CustomerSegmentDto> Segments { get; set; } = new();
}

/// <summary>
/// DTO cho khách hàng top
/// </summary>
public class TopCustomerDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal TotalSpent { get; set; }           // Tổng chi tiêu
    public string RiskLevel { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }
}

/// <summary>
/// DTO cho phân khúc khách hàng
/// </summary>
public class CustomerSegmentDto
{
    public string Segment { get; set; } = string.Empty;    // "VIP", "Thường xuyên", "Mới"
    public int Count { get; set; }
    public decimal AverageSpent { get; set; }
    public decimal TotalRevenue { get; set; }
}
