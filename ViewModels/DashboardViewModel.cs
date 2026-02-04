using UCar.Models;
using UCar.Models.Enums;

namespace UCar.ViewModels;

public class DashboardViewModel
{
    // Stats for Admin/Staff
    public int TotalCustomers { get; set; }
    public int TotalActiveVehicles { get; set; }
    public int TodayBookings { get; set; }
    public decimal MonthlyRevenue { get; set; }

    // Vehicle status counts
    public int RentingVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public int AvailableVehicles { get; set; }

    // Popular vehicles
    public List<VehicleSummary> PopularVehicles { get; set; } = new();

    // Recent bookings
    public List<BookingSummary> RecentBookings { get; set; } = new();
    public int NewBookingsCount { get; set; }

    // Statistics
    public int MonthlyBookingsCount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageRating { get; set; }
}

public class VehicleSummary
{
    public Guid VehicleId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string FullName => $"{Make} {ModelName}";
    public string TypeName { get; set; } = string.Empty;
    public int Seats { get; set; }
    public string? Transmission { get; set; }
    public VehicleStatus Status { get; set; }
    public decimal DailyPrice { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string? ImageFileName { get; set; }
}

public class BookingSummary
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerInitials { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; }
    public decimal EstimatedTotal { get; set; }
}
