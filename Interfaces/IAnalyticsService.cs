using UCar.Models.DTOs.Analytics;

namespace UCar.Interfaces;

/// <summary>
/// Service cho báo cáo và phân tích (Module 9)
/// Tương ứng DFD 9.0: Báo cáo & phân tích
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Lấy báo cáo doanh thu, lợi nhuận và chi phí (DFD 9.1)
    /// </summary>
    /// <param name="fromDate">Từ ngày</param>
    /// <param name="toDate">Đến ngày</param>
    /// <returns>Báo cáo doanh thu chi tiết</returns>
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Lấy báo cáo tỷ lệ sử dụng xe (DFD 9.2)
    /// </summary>
    /// <returns>Báo cáo utilization và performance xe</returns>
    Task<VehicleUtilizationReportDto> GetVehicleUtilizationReportAsync();

    /// <summary>
    /// Lấy báo cáo khách hàng (DFD 9.3)
    /// </summary>
    /// <param name="months">Số tháng để xác định khách hàng active (mặc định 6 tháng)</param>
    /// <returns>Báo cáo phân tích khách hàng</returns>
    Task<CustomerReportDto> GetCustomerReportAsync(int months = 6);
}
