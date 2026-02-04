using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models.DTOs.Analytics;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service implementation cho báo cáo và phân tích (Module 9)
/// Tương ứng DFD 9.0: Báo cáo & phân tích
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly UCarDbContext _context;
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        UCarDbContext context,
        IVehicleService vehicleService,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy báo cáo doanh thu, lợi nhuận và chi phí (DFD 9.1)
    /// </summary>
    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime fromDate, DateTime toDate)
    {
        // Đảm bảo toDate bao gồm cả ngày cuối cùng (23:59:59.999)
        toDate = toDate.Date.AddDays(1).AddTicks(-1);
        _logger.LogInformation("GetRevenueReportAsync: From {FromDate} to {ToDate}", fromDate, toDate);

        var report = new RevenueReportDto();

        // Lấy tất cả invoices trong khoảng thời gian
        var invoices = await _context.Invoices
            .Where(i => i.IssuedDate >= fromDate && i.IssuedDate <= toDate)
            .ToListAsync();

        // ===== DOANH THU TỪ INVOICES (không bao gồm đặt cọc) =====
        var revenueInvoices = invoices.Where(i => i.InvoiceType != InvoiceType.Deposit && i.InvoiceType != InvoiceType.Refund).ToList();
        
        // Nếu có Rental invoices → Dùng dữ liệu invoice
        // Nếu không có → Fallback sang dữ liệu từ Contracts (doanh thu dự kiến)
        if (revenueInvoices.Any())
        {
            report.TotalRevenue = revenueInvoices.Sum(i => i.TotalAmount);
            report.TotalPaid = revenueInvoices.Sum(i => i.AmountPaid);
            report.TotalDue = revenueInvoices.Sum(i => i.AmountDue);
        }
        else
        {
            // Fallback: Tính từ Contracts đã hoàn thành hoặc đang active/chờ quyết toán
            // PendingSettlement = xe đã trả, đang chờ thanh toán final invoice
            var contracts = await _context.RentalContracts
                .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
                .Where(c => c.Status == RentalContractStatus.Completed || 
                           c.Status == RentalContractStatus.Active || 
                           c.Status == RentalContractStatus.InProgress ||
                           c.Status == RentalContractStatus.PendingSettlement)
                .ToListAsync();
            
            report.TotalRevenue = contracts.Sum(c => c.TotalAmountFinal);
            // Ước tính đã thu = deposit đã thanh toán (từ deposit invoices đã paid)
            var depositsPaid = invoices.Where(i => i.InvoiceType == InvoiceType.Deposit).Sum(i => i.AmountPaid);
            report.TotalPaid = depositsPaid; // Chỉ có deposit đã thu
            report.TotalDue = report.TotalRevenue; // Tiền thuê chưa thu (chưa có rental invoice)
        }

        // Tiền thuê cơ bản (từ Rental invoices)
        var rentalInvoices = invoices.Where(i => i.InvoiceType == InvoiceType.Rental).ToList();
        report.RentalRevenue = rentalInvoices.Sum(i => i.BaseRentalAmount);

        // Phụ phí (từ Surcharge invoices + phần surcharge trong Rental)
        report.SurchargeRevenue = invoices
            .Where(i => i.InvoiceType == InvoiceType.Surcharge)
            .Sum(i => i.TotalAmount) + rentalInvoices.Sum(i => i.SurchargesTotal);

        // Phí phạt (từ Penalty invoices + phần penalty trong Rental)
        report.PenaltyRevenue = invoices
            .Where(i => i.InvoiceType == InvoiceType.Penalty)
            .Sum(i => i.TotalAmount) + rentalInvoices.Sum(i => i.PenaltiesTotal);

        // Thuế
        report.TaxRevenue = revenueInvoices.Sum(i => i.TaxAmount);

        // Giảm giá
        report.DiscountAmount = revenueInvoices.Sum(i => i.DiscountAmount);

        // Lấy tất cả contracts trong khoảng thời gian
        var allContracts = await _context.RentalContracts
            .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
            .ToListAsync();

        report.NormalDaysRevenue = allContracts.Sum(c => c.NormalDaysAmount);
        report.PeakDaysRevenue = allContracts.Sum(c => c.PeakDaysAmount);

        // ===== ĐẶT CỌC (Riêng biệt - không phải doanh thu) =====
        var depositInvoices = invoices.Where(i => i.InvoiceType == InvoiceType.Deposit).ToList();
        var refundInvoices = invoices.Where(i => i.InvoiceType == InvoiceType.Refund).ToList();
        
        // TotalAmount = số tiền cọc yêu cầu, dùng TotalAmount vì deposit invoices thường có AmountPaid = 0 ban đầu
        report.TotalDeposits = depositInvoices.Sum(i => i.TotalAmount);
        report.DepositsRefunded = refundInvoices.Sum(i => i.TotalAmount);
        report.DepositsHeld = report.TotalDeposits - report.DepositsRefunded;
        // Cọc bị khấu trừ (chuyển thành doanh thu) - cần tracking riêng nếu có
        report.DepositsForfeited = 0; // Sẽ implement khi có logic khấu trừ cọc

        // ===== THỐNG KÊ HỢP ĐỒNG =====
        report.TotalContracts = allContracts.Count;
        report.CompletedContracts = allContracts.Count(c => c.Status == RentalContractStatus.Completed);
        report.ActiveContracts = allContracts.Count(c => 
            c.Status == RentalContractStatus.Active || 
            c.Status == RentalContractStatus.InProgress);

        // Doanh thu theo tháng - ưu tiên invoices, fallback sang contracts
        if (revenueInvoices.Any())
        {
            // Có rental invoices → tính từ invoices đã load (không cần query lại)
            var monthlyData = revenueInvoices
                .GroupBy(i => new { i.IssuedDate.Year, i.IssuedDate.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = $"{g.Key.Month:00}/{g.Key.Year}",
                    Revenue = g.Sum(i => i.TotalAmount),
                    Contracts = g.Select(i => i.ContractId).Distinct().Count()
                })
                .OrderBy(m => m.Month)
                .ToList();

            _logger.LogInformation("Monthly data from invoices: {Count} months", monthlyData.Count);
            report.MonthlyData = monthlyData;
        }
        else
        {
            // Không có rental invoices → tính từ contracts (doanh thu dự kiến)
            var monthlyData = allContracts
                .Where(c => c.Status == RentalContractStatus.Completed || 
                           c.Status == RentalContractStatus.Active || 
                           c.Status == RentalContractStatus.InProgress ||
                           c.Status == RentalContractStatus.PendingSettlement)
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = $"{g.Key.Month:00}/{g.Key.Year}",
                    Revenue = g.Sum(c => c.TotalAmountFinal),
                    Contracts = g.Count()
                })
                .OrderBy(m => m.Month)
                .ToList();

            report.MonthlyData = monthlyData;
        }

        _logger.LogInformation("GetRevenueReportAsync: Total Revenue = {Revenue:N0} VNĐ (excluding deposits)", report.TotalRevenue);

        return report;
    }

    /// <summary>
    /// Lấy báo cáo tỷ lệ sử dụng xe (DFD 9.2)
    /// </summary>
    public async Task<VehicleUtilizationReportDto> GetVehicleUtilizationReportAsync()
    {
        _logger.LogInformation("GetVehicleUtilizationReportAsync: Starting");

        var report = new VehicleUtilizationReportDto();

        // Sử dụng VehicleService.GetVehicleStatsAsync() có sẵn
        var stats = await _vehicleService.GetVehicleStatsAsync();

        report.TotalVehicles = stats.TotalCount;
        report.AvailableVehicles = stats.AvailableCount;
        report.RentingVehicles = stats.RentingCount;
        report.MaintenanceVehicles = stats.MaintenanceCount;
        report.ReservedVehicles = stats.ReservedCount;
        report.OtherStatusVehicles = stats.ImpoundedCount + stats.IncidentCount;

        // Tính tỷ lệ sử dụng (xe đang thuê / tổng xe active)
        var activeVehicles = report.TotalVehicles; // Không tính xe Decommissioned
        if (activeVehicles > 0)
        {
            report.UtilizationRate = Math.Round((decimal)report.RentingVehicles / activeVehicles * 100, 2);
            report.AvailabilityRate = Math.Round((decimal)report.AvailableVehicles / activeVehicles * 100, 2);
        }

        // Top 10 xe theo doanh thu
        var topVehicles = await _context.Vehicles
            .Include(v => v.Model)
            .Where(v => v.CurrentStatus != VehicleStatus.Decommissioned)
            .Select(v => new VehiclePerformanceDto
            {
                VehicleId = v.VehicleId,
                PlateNo = v.PlateNo,
                Model = $"{v.Model.Make} {v.Model.ModelName}",
                TotalRentals = v.RentalContracts.Count(rc => rc.Status == RentalContractStatus.Completed),
                TotalRevenue = v.RentalContracts
                    .Where(rc => rc.Status == RentalContractStatus.Completed)
                    .Sum(rc => rc.TotalAmountFinal),
                TotalDays = v.RentalContracts.Sum(rc => rc.RentalDays)
            })
            .Where(v => v.TotalRentals > 0) // Chỉ lấy xe có lịch sử thuê
            .OrderByDescending(v => v.TotalRevenue)
            .Take(10)
            .ToListAsync();

        // Tính doanh thu trung bình/ngày
        foreach (var vehicle in topVehicles)
        {
            if (vehicle.TotalDays > 0)
            {
                vehicle.AverageDailyRevenue = Math.Round(vehicle.TotalRevenue / vehicle.TotalDays, 0);
            }
        }

        report.TopVehicles = topVehicles;

        _logger.LogInformation("GetVehicleUtilizationReportAsync: Utilization Rate = {Rate}%", report.UtilizationRate);

        return report;
    }

    /// <summary>
    /// Lấy báo cáo khách hàng (DFD 9.3)
    /// </summary>
    public async Task<CustomerReportDto> GetCustomerReportAsync(int months = 6)
    {
        _logger.LogInformation("GetCustomerReportAsync: Last {Months} months", months);

        var report = new CustomerReportDto();
        var cutoffDate = DateTime.Today.AddMonths(-months);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        // Tổng số khách hàng
        report.TotalCustomers = await _context.Customers.CountAsync();

        // Khách hàng active (có booking hoặc contract trong X tháng)
        report.ActiveCustomers = await _context.Customers
            .Where(c => 
                c.Bookings.Any(b => b.CreatedAt >= cutoffDate) ||
                c.RentalContracts.Any(rc => rc.CreatedAt >= cutoffDate))
            .CountAsync();

        // Khách hàng blacklist
        report.BlacklistedCustomers = await _context.Customers
            .CountAsync(c => c.IsBlacklisted);

        // Khách hàng mới trong tháng
        report.NewCustomers = await _context.Customers
            .CountAsync(c => c.CreatedAt >= monthStart);

        // Tổng doanh thu từ tất cả khách hàng
        report.TotalRevenue = await _context.RentalContracts
            .Where(rc => rc.Status == RentalContractStatus.Completed)
            .SumAsync(rc => rc.TotalAmountFinal);

        // Giá trị trung bình/khách
        if (report.TotalCustomers > 0)
        {
            report.AverageCustomerValue = Math.Round(report.TotalRevenue / report.TotalCustomers, 0);
        }

        // Top 20 khách hàng theo chi tiêu
        var topCustomers = await _context.Customers
            .Include(c => c.UserAccount)
            .Select(c => new TopCustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerName = c.FullName,
                Phone = c.UserAccount.Phone ?? "",
                TotalBookings = c.Bookings.Count,
                CompletedBookings = c.Bookings.Count(b => b.Status == BookingStatus.Completed),
                TotalSpent = c.RentalContracts
                    .Where(rc => rc.Status == RentalContractStatus.Completed)
                    .Sum(rc => rc.TotalAmountFinal),
                RiskLevel = c.RiskLevel ?? "Bình thường",
                IsBlacklisted = c.IsBlacklisted
            })
            .Where(c => c.TotalSpent > 0) // Chỉ lấy khách đã có giao dịch
            .OrderByDescending(c => c.TotalSpent)
            .Take(20)
            .ToListAsync();

        report.TopCustomers = topCustomers;

        // Phân khúc khách hàng
        var allCustomers = await _context.Customers
            .Include(c => c.Bookings)
            .Include(c => c.RentalContracts)
            .ToListAsync();

        var segments = new List<CustomerSegmentDto>();

        // VIP: > 10 lần thuê
        var vipCustomers = allCustomers.Where(c => c.Bookings.Count > 10).ToList();
        segments.Add(new CustomerSegmentDto
        {
            Segment = "VIP (>10 lần thuê)",
            Count = vipCustomers.Count,
            AverageSpent = vipCustomers.Any() 
                ? Math.Round(vipCustomers.Average(c => 
                    c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                        .Sum(rc => rc.TotalAmountFinal)), 0)
                : 0,
            TotalRevenue = vipCustomers.Sum(c =>
                c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                    .Sum(rc => rc.TotalAmountFinal))
        });

        // Thường xuyên: 3-10 lần
        var frequentCustomers = allCustomers.Where(c => c.Bookings.Count >= 3 && c.Bookings.Count <= 10).ToList();
        segments.Add(new CustomerSegmentDto
        {
            Segment = "Thường xuyên (3-10 lần)",
            Count = frequentCustomers.Count,
            AverageSpent = frequentCustomers.Any()
                ? Math.Round(frequentCustomers.Average(c =>
                    c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                        .Sum(rc => rc.TotalAmountFinal)), 0)
                : 0,
            TotalRevenue = frequentCustomers.Sum(c =>
                c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                    .Sum(rc => rc.TotalAmountFinal))
        });

        // Mới: < 3 lần
        var newCustomers = allCustomers.Where(c => c.Bookings.Count < 3).ToList();
        segments.Add(new CustomerSegmentDto
        {
            Segment = "Mới (<3 lần)",
            Count = newCustomers.Count,
            AverageSpent = newCustomers.Any()
                ? Math.Round(newCustomers.Average(c =>
                    c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                        .Sum(rc => rc.TotalAmountFinal)), 0)
                : 0,
            TotalRevenue = newCustomers.Sum(c =>
                c.RentalContracts.Where(rc => rc.Status == RentalContractStatus.Completed)
                    .Sum(rc => rc.TotalAmountFinal))
        });

        report.Segments = segments;

        _logger.LogInformation("GetCustomerReportAsync: Total Customers = {Total}, Active = {Active}", 
            report.TotalCustomers, report.ActiveCustomers);

        return report;
    }
}
