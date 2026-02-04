using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels.Contract;

namespace UCar.Services;

/// <summary>
/// Service quản lý hợp đồng thuê xe
/// Tương ứng DFD 5.0: Quản lý hợp đồng thuê
/// </summary>
public class ContractService : IContractService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<ContractService> _logger;
    private readonly IBranchAccessService _branchAccess;
    private readonly IInvoiceService _invoiceService;
    private readonly IPriceCalculationService _priceCalculationService;

    // Điều khoản mặc định
    private const string DefaultTerms = @"ĐIỀU KHOẢN HỢP ĐỒNG THUÊ XE

1. BÊN THUÊ cam kết sử dụng xe đúng mục đích, tuân thủ luật giao thông.
2. BÊN THUÊ chịu trách nhiệm về mọi vi phạm giao thông trong thời gian thuê.
3. BÊN THUÊ không được cầm cố, thế chấp, cho thuê lại xe.
4. BÊN THUÊ phải trả xe đúng hạn, đúng địa điểm đã thỏa thuận.
5. Trường hợp trả xe trễ, BÊN THUÊ chịu phí phạt theo quy định.
6. BÊN THUÊ chịu trách nhiệm bồi thường nếu xe bị hư hỏng do lỗi của BÊN THUÊ.
7. Tiền cọc sẽ được hoàn trả sau khi đối soát và xe không có vấn đề.
8. BÊN CHO THUÊ có quyền thu hồi xe nếu BÊN THUÊ vi phạm điều khoản.";

    public ContractService(UCarDbContext context, ILogger<ContractService> logger, IBranchAccessService branchAccess, IInvoiceService invoiceService, IPriceCalculationService priceCalculationService)
    {
        _context = context;
        _logger = logger;
        _branchAccess = branchAccess;
        _invoiceService = invoiceService;
        _priceCalculationService = priceCalculationService;
    }

    #region 5.1 Tạo và quản lý hợp đồng thuê

    public async Task<ContractListResultViewModel> GetContractsAsync(ContractSearchViewModel filter)
    {
        _logger.LogInformation("GetContractsAsync: Fetching contracts with filter {@Filter}", filter);

        var query = _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .Include(c => c.Handler)
            .Include(c => c.Booking)
            .AsQueryable();

        // *** BRANCH ACCESS FILTER ***
        // BranchManager và Staff chỉ thấy hợp đồng của xe thuộc chi nhánh mình
        var userBranchId = _branchAccess.GetCurrentUserBranchId();
        if (userBranchId.HasValue)
        {
            query = query.Where(c => c.Vehicle.BranchId == userBranchId.Value);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(c =>
                c.ContractCode.ToLower().Contains(keyword) ||
                c.Customer.FullName.ToLower().Contains(keyword) ||
                (c.Customer.UserAccount.Phone != null && c.Customer.UserAccount.Phone.Contains(keyword)) ||
                c.Vehicle.PlateNo.ToLower().Contains(keyword)
            );
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(c => c.PlannedStart >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(c => c.PlannedEnd <= filter.ToDate.Value);
        }

        if (filter.BranchId.HasValue)
        {
            query = query.Where(c => c.Vehicle.BranchId == filter.BranchId.Value);
        }

        if (filter.VehicleId.HasValue)
        {
            query = query.Where(c => c.VehicleId == filter.VehicleId.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == filter.CustomerId.Value);
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy switch
        {
            "PlannedStart" => filter.SortDesc ? query.OrderByDescending(c => c.PlannedStart) : query.OrderBy(c => c.PlannedStart),
            "Status" => filter.SortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "TotalAmount" => filter.SortDesc ? query.OrderByDescending(c => c.TotalAmountFinal) : query.OrderBy(c => c.TotalAmountFinal),
            _ => filter.SortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new ContractListViewModel
            {
                ContractId = c.ContractId,
                ContractCode = c.ContractCode,
                CustomerName = c.Customer.FullName,
                CustomerPhone = c.Customer.UserAccount.Phone ?? "",
                VehiclePlateNo = c.Vehicle.PlateNo,
                VehicleModel = c.Vehicle.Model.Make + " " + c.Vehicle.Model.ModelName,
                PlannedStart = c.PlannedStart,
                PlannedEnd = c.PlannedEnd,
                RentalDays = c.RentalDays,
                TotalAmount = c.TotalAmountFinal,
                DepositAmount = c.SnapshotDepositAmount,
                Status = c.Status,
                BookingId = c.BookingId,
                CreatedAt = c.CreatedAt,
                CreatedByName = c.Handler.Username,
                // Handover Integration: Check if contract is ready for handover
                IsBookingCancelled = c.Booking != null && c.Booking.Status == BookingStatus.Cancelled,
                // Luồng mới: Draft, PendingSigning = sẵn sàng giao xe
                IsReadyForHandover = (c.Status == RentalContractStatus.Draft || 
                                      c.Status == RentalContractStatus.PendingSigning) &&
                                     (c.Booking == null || c.Booking.Status != BookingStatus.Cancelled)
            })
            .ToListAsync();

        return new ContractListResultViewModel
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            Filter = filter,
            StatusOptions = GetStatusOptions(),
            BranchOptions = await GetBranchOptionsAsync()
        };
    }

    public async Task<ContractListResultViewModel> GetMyContractsAsync(Guid userId, ContractSearchViewModel filter)
    {
        _logger.LogInformation("GetMyContractsAsync: Fetching contracts for user {UserId}", userId);

        // Find customer by userId
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            return new ContractListResultViewModel { Filter = filter };
        }

        filter.CustomerId = customer.CustomerId;
        return await GetContractsAsync(filter);
    }

    public async Task<ContractDetailsViewModel?> GetContractDetailsAsync(Guid contractId, string? contractCode = null)
    {
        _logger.LogInformation("GetContractDetailsAsync: Fetching contract {ContractId}", contractId);

        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Customer).ThenInclude(cu => cu.Documents)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model).ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle).ThenInclude(v => v.Branch)
            .Include(c => c.Handler)
            .Include(c => c.Confirmer)
            .Include(c => c.Canceller)
            .Include(c => c.Booking)
            .Include(c => c.HandoverRecord)
            .Include(c => c.ReturnRecord)
            .Include(c => c.Charges)
            .Include(c => c.Violations)
            .FirstOrDefaultAsync(c => c.ContractId == contractId || (contractCode != null && c.ContractCode == contractCode));

        if (contract == null) return null;

        // Get ID number from documents
        var idDoc = contract.Customer.Documents
            .FirstOrDefault(d => d.DocType == CustomerDocumentType.IdCard || d.DocType == CustomerDocumentType.Passport);

        // Check if delivery invoice is paid (Luồng mới: thanh toán trước khi xác nhận)
        var deliveryInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Delivery);
        var hasPaidDeliveryInvoice = deliveryInvoice != null && deliveryInvoice.Status == InvoiceStatus.Paid;

        return new ContractDetailsViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            Status = contract.Status,

            // Customer
            CustomerId = contract.CustomerId,
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            CustomerEmail = contract.Customer.UserAccount.Email,
            CustomerAddress = contract.Customer.AddressText,
            CustomerIdNumber = idDoc?.DocNumber,

            // Vehicle
            VehicleId = contract.VehicleId,
            VehiclePlateNo = contract.Vehicle.PlateNo,
            VehicleModel = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            VehicleTypeName = contract.Vehicle.Model.VehicleType.TypeName,
            VehicleColor = contract.Vehicle.Color,
            VehicleYear = contract.Vehicle.ManufactureYear,
            BranchName = contract.Vehicle.Branch.Name,

            // Time
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            ActualStart = contract.ActualStart,
            ActualEnd = contract.ActualEnd,
            RentalDays = contract.RentalDays,
            ActualRentalDays = contract.ActualStart.HasValue && contract.ActualEnd.HasValue
                ? (int)(contract.ActualEnd.Value - contract.ActualStart.Value).TotalDays
                : null,

            // Location
            PickupLocation = contract.PickupLocation,
            ReturnLocation = contract.ReturnLocation,

            // Finance
            UnitPrice = contract.SnapshotUnitPrice,
            RentalAmount = contract.RentalAmount,
            ExtraCharges = contract.ExtraCharges,
            DepositAmount = contract.SnapshotDepositAmount,
            TotalAmount = contract.TotalAmountFinal,

            Charges = contract.Charges.Select(ch => new ChargeDetailViewModel
            {
                ChargeId = ch.ChargeId,
                ChargeType = ch.ChargeType.ToString(),
                Description = ch.Description ?? "",
                Amount = ch.Amount,
                IsPaid = ch.IsPaid,
                CreatedAt = ch.CreatedAt
            }).ToList(),

            // Terms
            Terms = contract.Terms,
            InternalNote = contract.InternalNote,

            // Sign/Confirm
            CustomerSigned = contract.CustomerSigned,
            CustomerSignedAt = contract.CustomerSignedAt,
            ConfirmedByName = contract.Confirmer?.Username,
            ConfirmedAt = contract.ConfirmedAt,

            // Cancel
            CancellationReason = contract.CancellationReason,
            CancelledByName = contract.Canceller?.Username,
            CancelledAt = contract.CancelledAt,

            // Links
            BookingId = contract.BookingId,
            BookingCode = contract.BookingId.HasValue ? $"BK-{contract.BookingId.Value.ToString()[..8].ToUpper()}" : null,
            HandoverId = contract.HandoverRecord?.HandoverId,
            HandedAt = contract.HandoverRecord?.HandedAt,
            ReturnId = contract.ReturnRecord?.ReturnId,
            ReturnedAt = contract.ReturnRecord?.ReturnedAt,

            Violations = contract.Violations.Select(v => new ViolationSummaryViewModel
            {
                ViolationId = v.ViolationId,
                ViolationType = v.ViolationType.ToString(),
                Description = v.Description,
                PenaltyAmount = v.PenaltyAmount,
                Status = v.Status.ToString(),
                DetectedAt = v.DetectedAt
            }).ToList(),

            // Audit
            CreatedAt = contract.CreatedAt,
            CreatedByName = contract.Handler.Username,
            UpdatedAt = contract.UpdatedAt,

            // Luồng mới: Thanh toán trước khi xác nhận
            HasHandoverRecord = contract.HandoverRecord != null,
            HasPaidDeliveryInvoice = hasPaidDeliveryInvoice
        };
    }

    public async Task<BookingForContractViewModel?> GetBookingForContractAsync(Guid bookingId)
    {
        _logger.LogInformation("GetBookingForContractAsync: Fetching booking {BookingId}", bookingId);

        var booking = await _context.Bookings
            .Include(b => b.Customer).ThenInclude(c => c.UserAccount)
            .Include(b => b.AssignedVehicle).ThenInclude(v => v!.Model).ThenInclude(m => m.VehicleType)
            .Include(b => b.VehicleType)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return null;

        // Check if already has contract
        var existingContract = await _context.RentalContracts
            .AnyAsync(c => c.BookingId == bookingId && c.Status != RentalContractStatus.Cancelled);
        
        if (existingContract)
        {
            _logger.LogWarning("Booking {BookingId} already has an active contract", bookingId);
            return null;
        }

        // Get price from assigned vehicle's model (since Prices.VehicleTypeId is nullable)
        Price? price = null;
        if (booking.AssignedVehicle?.ModelId != null)
        {
            price = await _context.Prices
                .Where(p => p.VehicleModelId == booking.AssignedVehicle.ModelId && p.IsActive)
                .FirstOrDefaultAsync();
        }

        return new BookingForContractViewModel
        {
            BookingId = booking.BookingId,
            BookingCode = $"BK-{booking.BookingId.ToString()[..8].ToUpper()}",
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer.FullName,
            CustomerPhone = booking.Customer.UserAccount.Phone ?? "",
            VehicleId = booking.AssignedVehicleId ?? Guid.Empty,
            VehiclePlateNo = booking.AssignedVehicle?.PlateNo ?? "",
            VehicleModel = booking.AssignedVehicle != null
                ? $"{booking.AssignedVehicle.Model.Make} {booking.AssignedVehicle.Model.ModelName}"
                : "",
            StartAt = booking.StartAt,
            EndAt = booking.EndAt,
            EstimatedTotal = booking.EstimatedTotal,
            PriceId = price?.PriceId ?? Guid.Empty,
            UnitPrice = price?.BaseDailyPrice ?? 0,
            DepositSuggest = 2000000 // Fixed responsibility deposit
        };
    }

    public async Task<ContractCreateOptionsViewModel> GetCreateOptionsAsync()
    {
        var customers = await _context.Customers
            .Include(c => c.UserAccount)
            .Include(c => c.Documents)
            .Where(c => !c.IsBlacklisted)
            .Select(c => new CustomerOption
            {
                CustomerId = c.CustomerId,
                FullName = c.FullName,
                Phone = c.UserAccount.Phone ?? "",
                IdNumber = c.Documents
                    .Where(d => d.DocType == CustomerDocumentType.IdCard || d.DocType == CustomerDocumentType.Passport)
                    .Select(d => d.DocNumber)
                    .FirstOrDefault(),
                IsBlacklisted = c.IsBlacklisted
            })
            .ToListAsync();

        var vehicles = await _context.Vehicles
            .Include(v => v.Model).ThenInclude(m => m.VehicleType)
            .Where(v => v.CurrentStatus == VehicleStatus.Available || v.CurrentStatus == VehicleStatus.Reserved)
            .Select(v => new VehicleOption
            {
                VehicleId = v.VehicleId,
                ModelId = v.Model.ModelId,
                PlateNo = v.PlateNo,
                ModelName = v.Model.Make + " " + v.Model.ModelName,
                VehicleTypeId = v.Model.VehicleTypeId,
                VehicleTypeName = v.Model.VehicleType.TypeName,
                IsAvailable = v.CurrentStatus == VehicleStatus.Available
            })
            .ToListAsync();

        var prices = await _context.Prices
            .Where(p => p.IsActive)
            .Select(p => new PriceOption
            {
                PriceId = p.PriceId,
                Name = p.Name,
                VehicleTypeId = p.VehicleTypeId, // Include to match with vehicle
                UnitPrice = p.BaseDailyPrice,
                DepositSuggest = 2000000 // Fixed
            })
            .ToListAsync();

        return new ContractCreateOptionsViewModel
        {
            Customers = customers,
            Vehicles = vehicles,
            Prices = prices,
            DefaultTerms = DefaultTerms
        };
    }

    public async Task<Guid> CreateContractAsync(ContractCreateViewModel model, Guid handledBy)
    {
        _logger.LogInformation("CreateContractAsync: Creating contract for customer {CustomerId}, vehicle {VehicleId}", 
            model.CustomerId, model.VehicleId);

        // Validate customer eligibility
        var (isEligible, reason) = await CheckCustomerEligibilityAsync(model.CustomerId);
        if (!isEligible)
        {
            throw new InvalidOperationException($"Khách hàng không đủ điều kiện thuê xe: {reason}");
        }

        // // Validate vehicle availability
        // var isAvailable = await CheckVehicleAvailabilityAsync(model.VehicleId, model.PlannedStart, model.PlannedEnd);
        // if (!isAvailable)
        // {
        //     throw new InvalidOperationException("Xe không khả dụng trong khoảng thời gian này");
        // } chỗ này để Trí sửaaaa


        // Validate dates
        if (model.PlannedStart >= model.PlannedEnd)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu");
        }

        if (model.PlannedStart < DateTime.Now.AddHours(-1))
        {
            throw new InvalidOperationException("Ngày bắt đầu không hợp lệ");
        }

        // Get vehicle and model for price calculation
        var vehicle = await _context.Vehicles
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.VehicleId == model.VehicleId);
        
        if (vehicle == null)
        {
            throw new InvalidOperationException("Không tìm thấy xe");
        }

        // Calculate detailed pricing using IPriceCalculationService
        var priceEstimate = await _priceCalculationService.CalculateEstimateAsync(
            vehicle.Model.ModelId, model.PlannedStart, model.PlannedEnd);

        // Generate contract code
        var contractCode = await GenerateContractCodeAsync();

        var contract = new RentalContract
        {
            ContractId = Guid.NewGuid(),
            ContractCode = contractCode,
            BookingId = model.BookingId,
            CustomerId = model.CustomerId,
            VehicleId = model.VehicleId,
            PriceId = model.PriceId,
            SnapshotUnitPrice = model.UnitPrice,
            SnapshotDepositAmount = model.DepositAmount,
            PlannedStart = model.PlannedStart,
            PlannedEnd = model.PlannedEnd,
            PickupLocation = model.PickupLocation,
            ReturnLocation = model.ReturnLocation,
            RentalDays = priceEstimate!.TotalDays,
            NormalDays = priceEstimate.NormalDays,
            PeakDays = priceEstimate.PeakDays,
            NormalDaysAmount = priceEstimate.NormalDaysAmount,
            PeakDaysAmount = priceEstimate.PeakDaysAmount,
            MonthlyAmount = priceEstimate.MonthlyAmount,
            IsMonthlyRate = priceEstimate.MonthlyAmount > 0,
            RentalAmount = priceEstimate.SubTotal,
            ExtraCharges = model.ExtraCharges,
            ResponsibilityDeposit = priceEstimate.ResponsibilityDeposit,
            RentalDeposit = priceEstimate.RentalDeposit,
            TotalAmountFinal = priceEstimate.TotalAmount + model.ExtraCharges,
            // Luồng mới: Tạo hợp đồng luôn ở trạng thái Draft, sau đó giao xe → PendingSigning → Active
            Status = RentalContractStatus.Draft,
            Terms = string.IsNullOrWhiteSpace(model.Terms) ? DefaultTerms : model.Terms,
            InternalNote = model.InternalNote,
            HandledBy = handledBy,
            CreatedAt = DateTime.Now
        };

        _context.RentalContracts.Add(contract);

        // Update booking status if created from booking
        if (model.BookingId.HasValue)
        {
            var booking = await _context.Bookings.FindAsync(model.BookingId.Value);
            if (booking != null)
            {
                booking.Status = BookingStatus.InProgress;
            }
        }

        // Update vehicle status (already fetched above)
        vehicle.CurrentStatus = VehicleStatus.Reserved;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created contract {ContractCode} with ID {ContractId}", contractCode, contract.ContractId);

        return contract.ContractId;
    }

    public async Task<ContractEditViewModel?> GetContractForEditAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return null;

        // Luồng mới: Chỉ cho phép edit Draft, PendingSigning
        if (contract.Status != RentalContractStatus.Draft && contract.Status != RentalContractStatus.PendingSigning)
        {
            _logger.LogWarning("Cannot edit contract {ContractId} with status {Status}", contractId, contract.Status);
            return null;
        }

        return new ContractEditViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CurrentStatus = contract.Status,
            CustomerId = contract.CustomerId,
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone,
            VehicleId = contract.VehicleId,
            VehiclePlateNo = contract.Vehicle.PlateNo,
            VehicleModel = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            PickupLocation = contract.PickupLocation,
            ReturnLocation = contract.ReturnLocation,
            PriceId = contract.PriceId,
            UnitPrice = contract.SnapshotUnitPrice,
            RentalDays = contract.RentalDays,
            RentalAmount = contract.RentalAmount,
            ExtraCharges = contract.ExtraCharges,
            DepositAmount = contract.SnapshotDepositAmount,
            TotalAmount = contract.TotalAmountFinal,
            Terms = contract.Terms,
            InternalNote = contract.InternalNote,
            RowVersion = contract.RowVersion
        };
    }

    public async Task<bool> UpdateContractAsync(ContractEditViewModel model, Guid updatedBy)
    {
        _logger.LogInformation("UpdateContractAsync: Updating contract {ContractId}", model.ContractId);

        var contract = await _context.RentalContracts.FindAsync(model.ContractId);
        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found", model.ContractId);
            return false;
        }

        // Luồng mới: Chỉ cho phép edit Draft, PendingSigning
        if (contract.Status != RentalContractStatus.Draft && contract.Status != RentalContractStatus.PendingSigning)
        {
            _logger.LogWarning("Cannot edit contract {ContractId} with status {Status}", model.ContractId, contract.Status);
            return false;
        }

        // If changing vehicle or dates, check availability
        if (contract.VehicleId != model.VehicleId || 
            contract.PlannedStart != model.PlannedStart || 
            contract.PlannedEnd != model.PlannedEnd)
        {
            var isAvailable = await CheckVehicleAvailabilityAsync(
                model.VehicleId, model.PlannedStart, model.PlannedEnd, contract.ContractId);
            if (!isAvailable)
            {
                throw new InvalidOperationException("Xe không khả dụng trong khoảng thời gian này");
            }
        }

        // Get vehicle and model for price calculation
        var vehicle = await _context.Vehicles
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.VehicleId == model.VehicleId);
        
        if (vehicle == null)
        {
            throw new InvalidOperationException("Không tìm thấy xe");
        }

        // Calculate detailed pricing using IPriceCalculationService
        var priceEstimate = await _priceCalculationService.CalculateEstimateAsync(
            vehicle.Model.ModelId, model.PlannedStart, model.PlannedEnd);

        // Update fields
        contract.CustomerId = model.CustomerId;
        contract.VehicleId = model.VehicleId;
        contract.PriceId = model.PriceId;
        contract.PlannedStart = model.PlannedStart;
        contract.PlannedEnd = model.PlannedEnd;
        contract.PickupLocation = model.PickupLocation;
        contract.ReturnLocation = model.ReturnLocation;
        contract.SnapshotUnitPrice = model.UnitPrice;
        contract.SnapshotDepositAmount = model.DepositAmount;
        contract.RentalDays = priceEstimate!.TotalDays;
        contract.NormalDays = priceEstimate.NormalDays;
        contract.PeakDays = priceEstimate.PeakDays;
        contract.NormalDaysAmount = priceEstimate.NormalDaysAmount;
        contract.PeakDaysAmount = priceEstimate.PeakDaysAmount;
        contract.MonthlyAmount = priceEstimate.MonthlyAmount;
        contract.IsMonthlyRate = priceEstimate.MonthlyAmount > 0;
        contract.RentalAmount = priceEstimate.SubTotal;
        contract.ExtraCharges = model.ExtraCharges;
        contract.ResponsibilityDeposit = priceEstimate.ResponsibilityDeposit;
        contract.RentalDeposit = priceEstimate.RentalDeposit;
        contract.TotalAmountFinal = priceEstimate.TotalAmount + model.ExtraCharges;
        contract.Terms = model.Terms;
        contract.InternalNote = model.InternalNote;
        contract.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated contract {ContractId}", model.ContractId);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating contract {ContractId}", model.ContractId);
            throw new InvalidOperationException("Hợp đồng đã được cập nhật bởi người khác. Vui lòng tải lại trang.");
        }
    }

    #endregion

    #region Sign / Confirm - Luồng mới: Ký giấy tại quầy

    /// <summary>
    /// Luồng mới: Khách không xem hợp đồng online, ký tại quầy
    /// Method này giữ lại để backward compatible nhưng không dùng trong luồng mới
    /// </summary>
    public async Task<ContractSignViewModel?> GetContractForSignAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        // Luồng mới: PendingSigning = đã lập biên bản, chờ ký tại quầy
        if (contract == null || contract.Status != RentalContractStatus.PendingSigning)
            return null;

        return new ContractSignViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            VehicleInfo = $"{contract.Vehicle.PlateNo} - {contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            TotalAmount = contract.TotalAmountFinal,
            DepositAmount = contract.SnapshotDepositAmount,
            Terms = contract.Terms
        };
    }

    /// <summary>
    /// Luồng mới: Ký giấy tại quầy, không phải online
    /// Method này giữ lại cho backward compatible
    /// </summary>
    public async Task<bool> SignContractAsync(Guid contractId, Guid userId)
    {
        _logger.LogInformation("SignContractAsync: User {UserId} signing contract {ContractId}", userId, contractId);

        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found", contractId);
            return false;
        }

        // Luồng mới: Chỉ ký được khi đang ở PendingSigning
        if (contract.Status != RentalContractStatus.PendingSigning)
        {
            _logger.LogWarning("Cannot sign contract {ContractId} with status {Status}", contractId, contract.Status);
            return false;
        }

        // Verify user is the customer
        if (contract.Customer.UserId != userId)
        {
            _logger.LogWarning("User {UserId} is not the customer of contract {ContractId}", userId, contractId);
            return false;
        }

        contract.CustomerSigned = true;
        contract.CustomerSignedAt = DateTime.Now;
        // Luồng mới: Sau khi ký + thanh toán → Active (do PaymentService xử lý)
        contract.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} signed by customer", contractId);
        return true;
    }

    public async Task<ContractConfirmViewModel?> GetContractForConfirmAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return null;

        // Luồng mới: Confirm khi PendingSigning (đã lập biên bản)
        if (contract.Status != RentalContractStatus.PendingSigning)
        {
            return null;
        }

        return new ContractConfirmViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            VehicleInfo = $"{contract.Vehicle.PlateNo} - {contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            TotalAmount = contract.TotalAmountFinal,
            DepositAmount = contract.SnapshotDepositAmount,
            CustomerHasSigned = contract.CustomerSigned,
            CustomerSignedAt = contract.CustomerSignedAt
        };
    }

    public async Task<bool> ConfirmContractAsync(Guid contractId, Guid confirmedBy, string? note = null)
    {
        _logger.LogInformation("ConfirmContractAsync: Staff {StaffId} confirming contract {ContractId}", confirmedBy, contractId);

        var contract = await _context.RentalContracts.FindAsync(contractId);
        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found", contractId);
            return false;
        }

        // Luồng mới: Confirm từ PendingSigning (sau khi lập biên bản giao xe)
        if (contract.Status != RentalContractStatus.PendingSigning)
        {
            _logger.LogWarning("Cannot confirm contract {ContractId} with status {Status}", contractId, contract.Status);
            return false;
        }

        contract.ConfirmedBy = confirmedBy;
        contract.ConfirmedAt = DateTime.Now;
        contract.CustomerSigned = true;
        contract.CustomerSignedAt = DateTime.Now;
        contract.Status = RentalContractStatus.Active;
        contract.ActualStart = DateTime.Now; // Ghi nhận thời gian bắt đầu thực tế
        contract.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(note))
        {
            contract.InternalNote = string.IsNullOrWhiteSpace(contract.InternalNote) 
                ? $"[Xác nhận HĐ] {note}" 
                : $"{contract.InternalNote}\n[Xác nhận HĐ] {note}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} confirmed, status changed to Active", contractId);

        // NOTE: Theo luồng mới, thanh toán được thực hiện tại quầy khi giao xe
        // Không tự tạo invoice ở đây nữa

        return true;
    }

    /// <summary>
    /// Tạo hợp đồng nháp (Draft) từ Booking đã xác nhận - Luồng mới
    /// </summary>
    public async Task<Guid> CreateDraftContractFromBookingAsync(Guid bookingId, Guid staffId)
    {
        _logger.LogInformation("CreateDraftContractFromBookingAsync: Creating draft contract from booking {BookingId}", bookingId);

        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.AssignedVehicle)
                .ThenInclude(v => v!.Model)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
            throw new InvalidOperationException("Không tìm thấy booking");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Chỉ có thể tạo hợp đồng từ booking đã xác nhận");

        if (booking.AssignedVehicleId == null || booking.AssignedVehicle == null)
            throw new InvalidOperationException("Booking chưa được gán xe");

        // Check if already has contract
        var existingContract = await _context.RentalContracts
            .AnyAsync(c => c.BookingId == bookingId && c.Status != RentalContractStatus.Cancelled);
        
        if (existingContract)
            throw new InvalidOperationException("Booking này đã có hợp đồng");

        // Get price from vehicle's model
        var price = await _context.Prices
            .Where(p => p.VehicleModelId == booking.AssignedVehicle.ModelId && p.IsActive)
            .FirstOrDefaultAsync();

        if (price == null)
            throw new InvalidOperationException("Không tìm thấy bảng giá cho loại xe này");

        // Calculate detailed pricing using IPriceCalculationService
        var priceEstimate = await _priceCalculationService.CalculateEstimateAsync(
            booking.AssignedVehicle.ModelId, booking.StartAt, booking.EndAt);

        // Generate contract code
        var contractCode = await GenerateContractCodeAsync();

        var contract = new RentalContract
        {
            ContractId = Guid.NewGuid(),
            ContractCode = contractCode,
            BookingId = bookingId,
            CustomerId = booking.CustomerId,
            VehicleId = booking.AssignedVehicleId.Value,
            PriceId = price.PriceId,
            
            // Snapshot giá tại thời điểm tạo hợp đồng
            SnapshotBaseDailyPrice = price.BaseDailyPrice,
            SnapshotMonthMultiplier = price.MonthMultiplier,
            SnapshotPeakMultiplier = price.PeakMultiplier,
            SnapshotOvertimeHourlyPrice = price.OvertimeHourlyPrice,
            SnapshotUnitPrice = price.BaseDailyPrice,
            SnapshotDepositAmount = priceEstimate!.TotalDeposit,
            
            PlannedStart = booking.StartAt,
            PlannedEnd = booking.EndAt,
            
            // Price breakdown
            RentalDays = priceEstimate.TotalDays,
            NormalDays = priceEstimate.NormalDays,
            PeakDays = priceEstimate.PeakDays,
            NormalDaysAmount = priceEstimate.NormalDaysAmount,
            PeakDaysAmount = priceEstimate.PeakDaysAmount,
            MonthlyAmount = priceEstimate.MonthlyAmount,
            IsMonthlyRate = priceEstimate.MonthlyAmount > 0,
            
            // Deposit breakdown
            ResponsibilityDeposit = priceEstimate.ResponsibilityDeposit,
            RentalDeposit = priceEstimate.RentalDeposit,
            
            // Amounts
            RentalAmount = priceEstimate.SubTotal,
            ExtraCharges = 0, // Phụ kiện sẽ được thêm khi giao xe
            TotalAmountFinal = priceEstimate.TotalAmount,
            
            // Status: LUÔN LÀ DRAFT theo luồng mới
            Status = RentalContractStatus.Draft,
            CustomerSigned = false,
            
            Terms = DefaultTerms,
            HandledBy = staffId,
            CreatedAt = DateTime.Now
        };

        _context.RentalContracts.Add(contract);

        // Update booking status
        booking.Status = BookingStatus.InProgress;

        // Reserve the vehicle
        var vehicle = booking.AssignedVehicle;
        if (vehicle.CurrentStatus == VehicleStatus.Available)
        {
            vehicle.CurrentStatus = VehicleStatus.Reserved;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created draft contract {ContractCode} from booking {BookingId}", contractCode, bookingId);

        return contract.ContractId;
    }

    /// <summary>
    /// Cập nhật hợp đồng khi giao xe - thêm phụ kiện và cập nhật tổng tiền
    /// </summary>
    public async Task UpdateContractForHandoverAsync(Guid contractId, decimal extraCharges, string? note = null)
    {
        var contract = await _context.RentalContracts.FindAsync(contractId);
        if (contract == null)
            throw new InvalidOperationException("Không tìm thấy hợp đồng");

        if (contract.Status != RentalContractStatus.Draft && contract.Status != RentalContractStatus.PendingSigning)
            throw new InvalidOperationException("Hợp đồng không ở trạng thái cho phép cập nhật");

        contract.ExtraCharges = extraCharges;
        contract.TotalAmountFinal = contract.RentalAmount + extraCharges;
        
        // Tính lại tiền cọc nếu tổng tiền thay đổi
        contract.RentalDeposit = contract.TotalAmountFinal * 0.5m;
        contract.SnapshotDepositAmount = contract.ResponsibilityDeposit + contract.RentalDeposit;

        if (!string.IsNullOrWhiteSpace(note))
        {
            contract.InternalNote = string.IsNullOrWhiteSpace(contract.InternalNote)
                ? $"[Giao xe] {note}"
                : $"{contract.InternalNote}\n[Giao xe] {note}";
        }

        contract.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Chuyển hợp đồng sang trạng thái chờ ký (PendingSigning)
    /// </summary>
    public async Task SetContractPendingSigningAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts.FindAsync(contractId);
        if (contract == null)
            throw new InvalidOperationException("Không tìm thấy hợp đồng");

        if (contract.Status != RentalContractStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể chuyển hợp đồng nháp sang chờ ký");

        contract.Status = RentalContractStatus.PendingSigning;
        contract.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Cancel

    public async Task<ContractCancelViewModel?> GetContractForCancelAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return null;

        // Luồng mới: Có thể hủy Draft, PendingSigning (trước khi thanh toán)
        var cancellableStatuses = new List<RentalContractStatus> 
        { 
            RentalContractStatus.Draft, 
            RentalContractStatus.PendingSigning
        };

        if (!cancellableStatuses.Contains(contract.Status))
        {
            return null;
        }

        // Check if has handover
        var hasHandover = await _context.HandoverRecords.AnyAsync(h => h.ContractId == contractId);
        if (hasHandover && contract.Status == RentalContractStatus.Active)
        {
            return null; // Cannot cancel after handover completed
        }

        return new ContractCancelViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CurrentStatus = contract.Status,
            CustomerName = contract.Customer.FullName,
            VehicleInfo = $"{contract.Vehicle.PlateNo} - {contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            DepositAmount = contract.SnapshotDepositAmount,
            HasDeposit = contract.SnapshotDepositAmount > 0,
            RefundAmount = contract.SnapshotDepositAmount // Full refund before handover
        };
    }

    public async Task<bool> CancelContractAsync(Guid contractId, Guid cancelledBy, string reason)
    {
        _logger.LogInformation("CancelContractAsync: Cancelling contract {ContractId} by {UserId}", contractId, cancelledBy);

        var contract = await _context.RentalContracts.FindAsync(contractId);
        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found", contractId);
            return false;
        }

        // Luồng mới: Chỉ hủy được Draft, PendingSigning
        var cancellableStatuses = new List<RentalContractStatus> 
        { 
            RentalContractStatus.Draft, 
            RentalContractStatus.PendingSigning
        };

        if (!cancellableStatuses.Contains(contract.Status))
        {
            _logger.LogWarning("Cannot cancel contract {ContractId} with status {Status}", contractId, contract.Status);
            return false;
        }

        // Check if has handover
        var hasHandover = await _context.HandoverRecords.AnyAsync(h => h.ContractId == contractId);
        if (hasHandover)
        {
            _logger.LogWarning("Cannot cancel contract {ContractId} - already has handover", contractId);
            return false;
        }

        contract.Status = RentalContractStatus.Cancelled;
        contract.CancellationReason = reason;
        contract.CancelledBy = cancelledBy;
        contract.CancelledAt = DateTime.Now;
        contract.UpdatedAt = DateTime.Now;

        // Release vehicle
        var vehicle = await _context.Vehicles.FindAsync(contract.VehicleId);
        if (vehicle != null && vehicle.CurrentStatus == VehicleStatus.Reserved)
        {
            vehicle.CurrentStatus = VehicleStatus.Available;
        }

        // Update booking if exists
        if (contract.BookingId.HasValue)
        {
            var booking = await _context.Bookings.FindAsync(contract.BookingId.Value);
            if (booking != null)
            {
                booking.Status = BookingStatus.Cancelled;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} cancelled", contractId);
        return true;
    }

    #endregion

    #region 5.3 Extend

    public async Task<ContractExtendViewModel?> GetContractForExtendAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return null;

        // Can only extend InProgress contracts
        if (contract.Status != RentalContractStatus.InProgress)
        {
            return null;
        }

        // Get daily rate from prices or use stored rental amount / days
        decimal dailyRate = contract.RentalDays > 0 
            ? contract.RentalAmount / contract.RentalDays 
            : 0;

        return new ContractExtendViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            VehicleInfo = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName} ({contract.Vehicle.PlateNo})",
            CurrentStartDate = contract.PlannedStart,
            CurrentEndDate = contract.PlannedEnd,
            DailyRate = dailyRate,
            NewEndDate = contract.PlannedEnd.AddDays(1)
        };
    }

    public async Task<bool> ExtendContractAsync(ContractExtendViewModel model, Guid extendedBy)
    {
        _logger.LogInformation("ExtendContractAsync: Extending contract {ContractId} to {NewEndDate}", 
            model.ContractId, model.NewEndDate);

        var contract = await _context.RentalContracts.FindAsync(model.ContractId);
        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found", model.ContractId);
            return false;
        }

        if (contract.Status != RentalContractStatus.InProgress)
        {
            _logger.LogWarning("Cannot extend contract {ContractId} with status {Status}", model.ContractId, contract.Status);
            return false;
        }

        if (model.NewEndDate <= contract.PlannedEnd)
        {
            throw new InvalidOperationException("Ngày kết thúc mới phải sau ngày kết thúc hiện tại");
        }

        // Check vehicle availability for extended period
        var isAvailable = await CheckVehicleAvailabilityAsync(
            contract.VehicleId, contract.PlannedEnd, model.NewEndDate, contract.ContractId);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Xe không khả dụng trong khoảng thời gian gia hạn");
        }

        // Calculate additional amount
        var additionalDays = (int)(model.NewEndDate - contract.PlannedEnd).TotalDays;
        var additionalAmount = additionalDays * contract.SnapshotUnitPrice;

        contract.PlannedEnd = model.NewEndDate;
        contract.RentalDays += additionalDays;
        contract.RentalAmount += additionalAmount;
        contract.TotalAmountFinal += additionalAmount;
        contract.UpdatedAt = DateTime.Now;

        // Add note
        contract.InternalNote = string.IsNullOrWhiteSpace(contract.InternalNote)
            ? $"[Gia hạn] +{additionalDays} ngày, +{additionalAmount:N0}đ. {model.ExtendNote}"
            : $"{contract.InternalNote}\n[Gia hạn] +{additionalDays} ngày, +{additionalAmount:N0}đ. {model.ExtendNote}";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} extended by {Days} days", model.ContractId, additionalDays);
        return true;
    }

    #endregion

    #region Print

    public async Task<ContractPrintViewModel?> GetContractForPrintAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Customer).ThenInclude(cu => cu.Documents)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model).ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle).ThenInclude(v => v.Branch)
            .Include(c => c.Handler)
            .Include(c => c.Confirmer)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null || contract.Status == RentalContractStatus.Draft)
            return null;

        var idDoc = contract.Customer.Documents
            .FirstOrDefault(d => d.DocType == CustomerDocumentType.IdCard || d.DocType == CustomerDocumentType.Passport);

        var dlDoc = contract.Customer.Documents
            .FirstOrDefault(d => d.DocType == CustomerDocumentType.License);

        return new ContractPrintViewModel
        {
            ContractCode = contract.ContractCode,
            PrintDate = DateTime.Now,
            Status = GetStatusDisplay(contract.Status),

            // Party A
            PartyAName = "CÔNG TY TNHH CHO THUÊ XE UCAR",
            PartyARepresentative = contract.Handler.Username,
            PartyAPosition = "Nhân viên",
            PartyAPhone = "1900 1234",

            // Party B (Customer)
            CustomerName = contract.Customer.FullName,
            CustomerDob = contract.Customer.Dob,
            CustomerIdNumber = idDoc?.DocNumber,
            CustomerIdIssueDate = idDoc?.IssuedDate?.ToString("dd/MM/yyyy"),
            CustomerIdIssuePlace = idDoc?.IssuedPlace,
            CustomerAddress = contract.Customer.AddressText,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            CustomerEmail = contract.Customer.UserAccount.Email,
            CustomerDriverLicense = dlDoc?.DocNumber,

            // Vehicle
            VehiclePlateNo = contract.Vehicle.PlateNo,
            VehicleBrand = contract.Vehicle.Model.Make,
            VehicleModel = contract.Vehicle.Model.ModelName,
            VehicleType = contract.Vehicle.Model.VehicleType.TypeName,
            VehicleYear = contract.Vehicle.ManufactureYear,
            VehicleColor = contract.Vehicle.Color,

            // Time & Location
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            RentalDays = contract.RentalDays,
            PickupLocation = contract.PickupLocation,
            ReturnLocation = contract.ReturnLocation,

            // Finance
            UnitPrice = contract.SnapshotUnitPrice,
            UnitPriceText = NumberToWords(contract.SnapshotUnitPrice),
            RentalAmount = contract.RentalAmount,
            ExtraCharges = contract.ExtraCharges,
            DepositAmount = contract.SnapshotDepositAmount,
            DepositAmountText = NumberToWords(contract.SnapshotDepositAmount),
            TotalAmount = contract.TotalAmountFinal,
            TotalAmountText = NumberToWords(contract.TotalAmountFinal),

            // Terms
            Terms = contract.Terms ?? "",

            // Sign
            CustomerSigned = contract.CustomerSigned,
            CustomerSignedAt = contract.CustomerSignedAt,
            ConfirmedByName = contract.Confirmer?.Username,
            ConfirmedAt = contract.ConfirmedAt,

            // Audit
            CreatedAt = contract.CreatedAt,
            CreatedByName = contract.Handler.Username
        };
    }

    #endregion

    #region Helpers

    public async Task<bool> CheckVehicleAvailabilityAsync(Guid vehicleId, DateTime start, DateTime end, Guid? excludeContractId = null)
    {
        // Check for overlapping contracts
        // Overlap: (StartA < EndB) && (EndA > StartB)
        // Luồng mới: Draft, PendingSigning, Active, InProgress đều block xe
        var blockingStatuses = new List<RentalContractStatus>
        {
            RentalContractStatus.Draft,
            RentalContractStatus.PendingSigning,
            RentalContractStatus.Active,
            RentalContractStatus.InProgress
        };

        var query = _context.RentalContracts
            .Where(c => c.VehicleId == vehicleId &&
                        blockingStatuses.Contains(c.Status) &&
                        c.PlannedStart < end &&
                        c.PlannedEnd > start);

        if (excludeContractId.HasValue)
        {
            query = query.Where(c => c.ContractId != excludeContractId.Value);
        }

        var hasOverlap = await query.AnyAsync();

        // Also check bookings
        var blockingBookingStatuses = new[] 
        { 
            BookingStatus.Pending, 
            BookingStatus.Confirmed, 
            BookingStatus.Deposited 
        };


        var hasBookingOverlap = await _context.Bookings
            .AnyAsync(b => b.AssignedVehicleId == vehicleId &&
                          blockingBookingStatuses.Contains(b.Status) &&
                          b.StartAt < end &&
                          b.EndAt > start);

        return !hasOverlap && !hasBookingOverlap;
    }

    public async Task<string> GenerateContractCodeAsync()
    {
        var today = DateTime.Today;
        var prefix = $"HD-{today:yyMM}";

        var lastContract = await _context.RentalContracts
            .Where(c => c.ContractCode.StartsWith(prefix))
            .OrderByDescending(c => c.ContractCode)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastContract != null)
        {
            var lastSeq = lastContract.ContractCode.Replace(prefix, "");
            if (int.TryParse(lastSeq, out int parsed))
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }

    public async Task<(bool isEligible, string? reason)> CheckCustomerEligibilityAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null)
        {
            return (false, "Không tìm thấy khách hàng");
        }

        if (customer.IsBlacklisted)
        {
            return (false, "Khách hàng nằm trong danh sách đen");
        }

        // Check for ID document
        var hasId = customer.Documents.Any(d => 
            d.DocType == CustomerDocumentType.IdCard || 
            d.DocType == CustomerDocumentType.Passport);

        if (!hasId)
        {
            return (false, "Khách hàng chưa có giấy tờ tùy thân");
        }

        // Check for driver license
        var hasLicense = customer.Documents.Any(d => d.DocType == CustomerDocumentType.License);
        if (!hasLicense)
        {
            return (false, "Khách hàng chưa có giấy phép lái xe");
        }

        return (true, null);
    }

    private string GetStatusDisplay(RentalContractStatus status)
    {
        return status switch
        {
            RentalContractStatus.Draft => "Bản nháp",
            RentalContractStatus.PendingSigning => "Chờ ký",
            RentalContractStatus.Active => "Đang hoạt động",
            RentalContractStatus.InProgress => "Đang thuê",
            RentalContractStatus.PendingSettlement => "Chờ quyết toán",
            RentalContractStatus.Completed => "Hoàn tất",
            RentalContractStatus.Disputed => "Tranh chấp",
            RentalContractStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    private List<StatusOption> GetStatusOptions()
    {
        return Enum.GetValues<RentalContractStatus>()
            .Select(s => new StatusOption
            {
                Value = s,
                Display = s switch
                {
                    RentalContractStatus.Draft => "Bản nháp",
                    RentalContractStatus.PendingSigning => "Chờ ký",
                    RentalContractStatus.Active => "Đang hoạt động",
                    RentalContractStatus.InProgress => "Đang thuê",
                    RentalContractStatus.PendingSettlement => "Chờ quyết toán",
                    RentalContractStatus.Completed => "Hoàn tất",
                    RentalContractStatus.Disputed => "Tranh chấp",
                    RentalContractStatus.Cancelled => "Đã hủy",
                    _ => s.ToString()
                }
            })
            .ToList();
    }

    private async Task<List<BranchOption>> GetBranchOptionsAsync()
    {
        return await _context.Branches
            .Select(b => new BranchOption
            {
                BranchId = b.BranchId,
                Name = b.Name
            })
            .ToListAsync();
    }

    private static string NumberToWords(decimal number)
    {
        // Simplified Vietnamese number to words
        var intPart = (long)number;
        if (intPart == 0) return "Không đồng";

        var units = new[] { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        var result = "";

        if (intPart >= 1000000)
        {
            result += $"{intPart / 1000000} triệu ";
            intPart %= 1000000;
        }

        if (intPart >= 1000)
        {
            result += $"{intPart / 1000} nghìn ";
            intPart %= 1000;
        }

        if (intPart > 0)
        {
            result += $"{intPart} ";
        }

        return result.Trim() + " đồng";
    }

    #endregion

    #region Handover Integration (Bridge)

    /// <summary>
    /// Statuses that indicate Contract is ready for Handover
    /// Luồng mới: Draft = chưa giao, PendingSigning = đã lập biên bản chờ ký
    /// </summary>
    private static readonly RentalContractStatus[] HandoverReadyStatuses = new[]
    {
        RentalContractStatus.Draft,
        RentalContractStatus.PendingSigning
    };

    /// <inheritdoc/>
    public async Task<bool> IsBookingReadyForHandoverAsync(Guid bookingId)
    {
        _logger.LogInformation("IsBookingReadyForHandoverAsync: Checking booking {BookingId}", bookingId);

        // Find Contract linked to this Booking
        var contract = await _context.RentalContracts
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);

        if (contract == null)
        {
            _logger.LogWarning("IsBookingReadyForHandoverAsync: No contract found for booking {BookingId}", bookingId);
            return false;
        }

        // Check if Booking is cancelled
        if (contract.Booking != null && contract.Booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogWarning("IsBookingReadyForHandoverAsync: Booking {BookingId} is cancelled", bookingId);
            return false;
        }

        // Check if Contract is in ready status for handover
        var isReady = HandoverReadyStatuses.Contains(contract.Status);
        _logger.LogInformation(
            "IsBookingReadyForHandoverAsync: Booking {BookingId} -> Contract {ContractId} Status={Status}, IsReady={IsReady}",
            bookingId, contract.ContractId, contract.Status, isReady);

        return isReady;
    }

    /// <inheritdoc/>
    public async Task<bool> IsContractReadyForHandoverAsync(Guid contractId)
    {
        _logger.LogInformation("IsContractReadyForHandoverAsync: Checking contract {ContractId}", contractId);

        var contract = await _context.RentalContracts
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
        {
            _logger.LogWarning("IsContractReadyForHandoverAsync: Contract {ContractId} not found", contractId);
            return false;
        }

        // If linked to a Booking, check Booking status
        if (contract.Booking != null && contract.Booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogWarning("IsContractReadyForHandoverAsync: Linked booking is cancelled");
            return false;
        }

        var isReady = HandoverReadyStatuses.Contains(contract.Status);
        _logger.LogInformation(
            "IsContractReadyForHandoverAsync: Contract {ContractId} Status={Status}, IsReady={IsReady}",
            contractId, contract.Status, isReady);

        return isReady;
    }

    /// <inheritdoc/>
    public async Task<ContractStatusViewModel?> GetContractStatusByBookingAsync(Guid bookingId)
    {
        _logger.LogInformation("GetContractStatusByBookingAsync: Getting status for booking {BookingId}", bookingId);

        var contract = await _context.RentalContracts
            .Include(c => c.Booking)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);

        if (contract == null)
        {
            return null;
        }

        return MapToContractStatusViewModel(contract);
    }

    /// <inheritdoc/>
    public async Task<ContractStatusViewModel?> GetContractStatusAsync(Guid contractId)
    {
        _logger.LogInformation("GetContractStatusAsync: Getting status for contract {ContractId}", contractId);

        var contract = await _context.RentalContracts
            .Include(c => c.Booking)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
        {
            return null;
        }

        return MapToContractStatusViewModel(contract);
    }

    private ContractStatusViewModel MapToContractStatusViewModel(RentalContract contract)
    {
        var isBookingCancelled = contract.Booking?.Status == BookingStatus.Cancelled;
        var isSigned = HandoverReadyStatuses.Contains(contract.Status);

        return new ContractStatusViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            BookingId = contract.BookingId,
            Status = contract.Status,
            StatusDisplay = GetStatusDisplayName(contract.Status),
            IsSigned = contract.CustomerSignedAt.HasValue,
            SignedAt = contract.CustomerSignedAt,
            IsConfirmed = contract.ConfirmedAt.HasValue,
            ConfirmedAt = contract.ConfirmedAt,
            IsReadyForHandover = isSigned && !isBookingCancelled,
            IsBookingCancelled = isBookingCancelled,
            TotalEstimatedCost = contract.TotalAmountFinal,
            CustomerName = contract.Customer?.FullName,
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            Message = GetStatusMessage(contract.Status, isBookingCancelled)
        };
    }

    private static string GetStatusDisplayName(RentalContractStatus status) => status switch
    {
        RentalContractStatus.Draft => "Bản nháp",
        RentalContractStatus.PendingSigning => "Chờ ký",
        RentalContractStatus.Active => "Đang hoạt động",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.PendingSettlement => "Chờ quyết toán",
        RentalContractStatus.Completed => "Hoàn tất",
        RentalContractStatus.Disputed => "Tranh chấp",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static string GetStatusMessage(RentalContractStatus status, bool isBookingCancelled)
    {
        if (isBookingCancelled)
            return "Không thể giao xe: Booking đã bị hủy.";

        return status switch
        {
            RentalContractStatus.Draft => "Hợp đồng đang ở bản nháp. Cần lập biên bản giao xe.",
            RentalContractStatus.PendingSigning => "Đã lập biên bản. Chờ khách ký và thanh toán.",
            RentalContractStatus.Active => "Hợp đồng đã ký. Khách đang thuê xe.",
            RentalContractStatus.InProgress => "Xe đã được giao. Đang trong quá trình thuê.",
            RentalContractStatus.PendingSettlement => "Chờ quyết toán sau khi trả xe.",
            RentalContractStatus.Cancelled => "Hợp đồng đã bị hủy.",
            _ => $"Trạng thái: {status}"
        };
    }

    #endregion
}
