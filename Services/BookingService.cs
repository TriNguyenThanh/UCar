using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.Booking;

namespace UCar.Services;

public class BookingService : IBookingService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(UCarDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<VehicleSearchResultVM>> SearchVehiclesAsync(DateTime start, DateTime end, Guid? typeId = null)
    {
        // Validate dates
        if (start < DateTime.Now.Date || end <= start) return new List<VehicleSearchResultVM>();

        // 1. Get active vehicles matching criteria
        // Chỉ lấy xe Available hoặc Reserved (có thể đặt trước)
        var query = _context.Vehicles
            .Include(v => v.Model)
            .Include(v => v.Model.VehicleType)
            .Include(v => v.Model.VehicleType.Prices)
            .Where(v => v.CurrentStatus == VehicleStatus.Available || 
                        v.CurrentStatus == VehicleStatus.Reserved);

        if (typeId.HasValue)
        {
            query = query.Where(v => v.Model.VehicleTypeId == typeId.Value);
        }

        var candidates = await query.ToListAsync();
        var results = new List<VehicleSearchResultVM>();

        // 2. Filter overlap bookings
        foreach (var v in candidates)
        {
            // Overlap check with active bookings (Pending, Confirmed, Deposited, InProgress)
            // Overlap: (StartA < EndB) && (EndA > StartB)
            var blockingStatuses = new[] 
            { 
                BookingStatus.Pending, 
                BookingStatus.Confirmed, 
                BookingStatus.Deposited,
                BookingStatus.InProgress 
            };
            bool isBusy = await _context.Bookings.AnyAsync(b => 
                b.AssignedVehicleId == v.VehicleId &&
                blockingStatuses.Contains(b.Status) &&
                b.StartAt < end && b.EndAt > start
            );

            if (!isBusy)
            {
                // Calculate Price
                var priceEntity = v.Model.VehicleType.Prices.FirstOrDefault(p => p.IsActive);
                decimal dailyPrice = priceEntity?.UnitPrice ?? 0;
                var days = (end - start).TotalDays;
                if (days < 1) days = 1;

                results.Add(new VehicleSearchResultVM
                {
                    VehicleId = v.VehicleId,
                    VehicleTypeId = v.Model.VehicleTypeId,
                    ModelName = $"{v.Model.Make} {v.Model.ModelName}",
                    VehicleTypeName = v.Model.VehicleType.TypeName,
                    PlateNo = v.PlateNo,
                    Color = v.Color ?? "N/A",
                    Year = v.ManufactureYear,
                    IsAvailable = true, // We filtered busy already
                    DailyPrice = dailyPrice,
                    EstimatedTotal = dailyPrice * (decimal)days,
                    Seats = v.Model.Seats, 
                    Transmission = v.Model.Transmission?.ToString() ?? "N/A",
                    ImageUrl = "https://placehold.co/600x400?text=" + v.Model.ModelName.Replace(" ", "+")
                });
            }
        }

        return results;
    }

    public async Task<VehicleSearchResultVM?> GetVehicleForBookingAsync(Guid vehicleId, DateTime start, DateTime end)
    {
        var v = await _context.Vehicles
            .Include(v => v.Model)
            .Include(v => v.Model.VehicleType)
            .Include(v => v.Model.VehicleType.Prices)
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId);

        if (v == null) return null;

        var priceEntity = v.Model.VehicleType.Prices.FirstOrDefault(p => p.IsActive);
        decimal dailyPrice = priceEntity?.UnitPrice ?? 0;
        var days = (int)Math.Ceiling((end - start).TotalDays);
        if (days < 1) days = 1;

        return new VehicleSearchResultVM
        {
            VehicleId = v.VehicleId,
            VehicleTypeId = v.Model.VehicleTypeId,
            ModelName = $"{v.Model.Make} {v.Model.ModelName}",
            VehicleTypeName = v.Model.VehicleType.TypeName,
            PlateNo = v.PlateNo,
            Color = v.Color ?? "N/A",
            Year = v.ManufactureYear,
            DailyPrice = dailyPrice,
            EstimatedTotal = dailyPrice * days,
            IsAvailable = true
        };
    }

    public async Task<Guid> CreateBookingAsync(Guid userId, BookingCreateVM model)
    {
        // Validations
        if (model.StartAt < DateTime.Now.AddHours(-1)) throw new ArgumentException("Thời gian bắt đầu không hợp lệ.");
        if (model.EndAt <= model.StartAt) throw new ArgumentException("Thời gian trả xe phải sau khi nhận.");

        var customer = await _context.Customers.Include(c => c.UserAccount).FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) throw new InvalidOperationException("Khách hàng không tồn tại hoặc chưa cập nhật hồ sơ.");

        // Re-check Availability
        var isAvailable = await CheckAvailabilityAsync(model.VehicleId, model.StartAt, model.EndAt);
        if (!isAvailable) throw new InvalidOperationException("Xe đã có người đặt trong khoảng thời gian này.");

        var vehicle = await _context.Vehicles.FindAsync(model.VehicleId);
        if (vehicle == null) throw new Exception("Vehicle not found");

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(),
            CustomerId = customer.CustomerId,
            VehicleTypeId = await _context.VehicleModels.Where(m => m.ModelId == vehicle.ModelId).Select(m => m.VehicleTypeId).FirstOrDefaultAsync(),
            AssignedVehicleId = model.VehicleId,
            StartAt = model.StartAt,
            EndAt = model.EndAt,
            Status = BookingStatus.Pending,
            EstimatedTotal = model.TotalAmount,
            CreatedBy = userId, // UserAccount Id
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Booking {booking.BookingId} created by User {userId}");
        return booking.BookingId;
    }

    public async Task<List<BookingListVM>> GetMyBookingsAsync(Guid userId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) return new List<BookingListVM>();

        return await _context.Bookings
            .Include(b => b.AssignedVehicle)
            .ThenInclude(v => v.Model)
            .Where(b => b.CustomerId == customer.CustomerId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingListVM
            {
                BookingId = b.BookingId,
                CustomerName = customer.FullName ?? "N/A",
                VehicleName = b.AssignedVehicle != null 
                    ? $"{b.AssignedVehicle.Model.Make} {b.AssignedVehicle.Model.ModelName} ({b.AssignedVehicle.PlateNo})" 
                    : "Chưa gán xe",
                StartAt = b.StartAt,
                EndAt = b.EndAt,
                TotalAmount = b.EstimatedTotal,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<BookingListVM>> GetAllBookingsAsync(BookingStatus? status = null, DateTime? fromDate = null)
    {
        var query = _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.AssignedVehicle)
            .ThenInclude(v => v.Model)
            .AsQueryable();

        if (status.HasValue) query = query.Where(b => b.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(b => b.StartAt >= fromDate.Value);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingListVM
            {
                BookingId = b.BookingId,
                CustomerName = b.Customer.FullName ?? "N/A",
                VehicleName = b.AssignedVehicle != null 
                    ? $"{b.AssignedVehicle.Model.Make} {b.AssignedVehicle.Model.ModelName} ({b.AssignedVehicle.PlateNo})" 
                    : "Chưa gán xe",
                StartAt = b.StartAt,
                EndAt = b.EndAt,
                TotalAmount = b.EstimatedTotal,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BookingDetailVM?> GetBookingDetailAsync(Guid bookingId, Guid userId, bool isAdminOrStaff)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .ThenInclude(c => c.UserAccount)
            .Include(b => b.AssignedVehicle)
            .ThenInclude(v => v.Model)
            .Include(b => b.VehicleType)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return null;

        // Security check
        if (!isAdminOrStaff && booking.Customer.UserId != userId) return null;

        return new BookingDetailVM
        {
            BookingId = booking.BookingId,
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer.FullName ?? "Unknow",
            CustomerPhone = booking.Customer.UserAccount.Phone ?? "",
            CustomerEmail = booking.Customer.UserAccount.Email ?? "",
            
            VehicleTypeId = booking.VehicleTypeId,
            VehicleTypeName = booking.VehicleType.TypeName,
            
            AssignedVehicleId = booking.AssignedVehicleId,
            AssignedVehicleName = booking.AssignedVehicle != null 
                ? $"{booking.AssignedVehicle.Model.Make} {booking.AssignedVehicle.Model.ModelName}" 
                : null,
            AssignedVehiclePlate = booking.AssignedVehicle?.PlateNo,
            
            StartAt = booking.StartAt,
            EndAt = booking.EndAt,
            TotalDays = (int)Math.Ceiling((booking.EndAt - booking.StartAt).TotalDays),
            EstimatedTotal = booking.EstimatedTotal,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            
            // Permissions Logic
            CanCancel = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed,
            CanApprove = isAdminOrStaff && booking.Status == BookingStatus.Pending,
            CanReject = isAdminOrStaff && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
        };
    }

    public async Task ConfirmBookingAsync(Guid bookingId, Guid staffId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) throw new Exception("Booking not found");

        if (booking.Status != BookingStatus.Pending) throw new InvalidOperationException("Chỉ có thể xác nhận đơn hàng đang chờ.");

        // Check availability again strictly
        if (booking.AssignedVehicleId.HasValue)
        {
            if (!await CheckAvailabilityAsync(booking.AssignedVehicleId.Value, booking.StartAt, booking.EndAt))
            {
                throw new InvalidOperationException("Xe đã bị trùng lịch trong lúc chờ xác nhận. Vui lòng chọn xe khác.");
            }
        }

        booking.Status = BookingStatus.Confirmed;
        // booking.HandlerId = staffId; // If we had such field on Booking, or log it
        
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Booking {bookingId} Confirmed by Staff {staffId}");
    }

    public async Task RejectBookingAsync(Guid bookingId, Guid staffId, string reason)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) throw new Exception("Booking not found");
        
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Chỉ có thể từ chối đơn đang chờ hoặc đã xác nhận.");
        
        booking.Status = BookingStatus.Rejected;
        
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Booking {bookingId} Rejected by Staff {staffId}. Reason: {reason}");
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, string reason)
    {
        var booking = await _context.Bookings.Include(b => b.Customer).FirstOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null) throw new Exception("Booking not found");

        // Chỉ cho phép hủy khi đang Pending hoặc Confirmed
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Không thể hủy đơn đã đặt cọc hoặc đang thực hiện.");
        
        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Booking {bookingId} Cancelled by User {userId}. Reason: {reason}");
    }

    public async Task<bool> CheckAvailabilityAsync(Guid vehicleId, DateTime start, DateTime end)
    {
        // Các trạng thái đang chiếm xe: Pending, Confirmed, Deposited, InProgress
        var blockingStatuses = new[] 
        { 
            BookingStatus.Pending, 
            BookingStatus.Confirmed, 
            BookingStatus.Deposited,
            BookingStatus.InProgress 
        };
        
        // Overlap formula: (StartA < EndB) && (EndA > StartB)
        bool overlap = await _context.Bookings.AnyAsync(b => 
            b.AssignedVehicleId == vehicleId &&
            blockingStatuses.Contains(b.Status) &&
            b.StartAt < end && b.EndAt > start
        );
        
        return !overlap;
    }
}
