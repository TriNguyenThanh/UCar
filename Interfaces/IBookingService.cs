using UCar.ViewModels.Booking;
using UCar.Models.Enums;

namespace UCar.Interfaces;

public interface IBookingService
{
    // 4.1 Tra cứu & Chọn xe
    Task<List<VehicleSearchResultVM>> SearchVehiclesAsync(
        DateTime start, 
        DateTime end, 
        Guid? vehicleTypeId = null, 
        string? make = null, 
        int? seats = null);
    Task<VehicleSearchResultVM?> GetVehicleForBookingAsync(Guid vehicleId, DateTime start, DateTime end);

    // 4.2 Tạo đặt xe
    Task<Guid> CreateBookingAsync(Guid userId, BookingCreateVM model);

    // 4.3 Quản lý danh sách
    Task<List<BookingListVM>> GetMyBookingsAsync(Guid userId);
    Task<List<BookingListVM>> GetAllBookingsAsync(BookingStatus? status = null, DateTime? fromDate = null);

    // 4.4 Chi tiết
    Task<BookingDetailVM?> GetBookingDetailAsync(Guid bookingId, Guid userId, bool isAdminOrStaff);

    // 4.5 & 4.6 Hành động
    Task ConfirmBookingAsync(Guid bookingId, Guid staffId);
    Task RejectBookingAsync(Guid bookingId, Guid staffId, string reason);
    Task CancelBookingAsync(Guid bookingId, Guid userId, string reason); // Customer or Staff
    
    // Helpers
    Task<bool> CheckAvailabilityAsync(Guid vehicleId, DateTime start, DateTime end, Guid? excludeBookingId = null);
}
