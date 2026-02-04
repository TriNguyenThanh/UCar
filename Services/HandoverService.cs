using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Handover;
using UCar.Models.DTOs.Vehicle;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service quản lý giao nhận xe
/// Tương ứng DFD 6.1-6.4
/// </summary>
public class HandoverService : IHandoverService
{
    private readonly UCarDbContext _context;
    private readonly IVehicleStatusService _vehicleStatusService;
    private readonly IBranchAccessService _branchAccess;
    private readonly IInvoiceService _invoiceService;

    public HandoverService(UCarDbContext context, IVehicleStatusService vehicleStatusService, IBranchAccessService branchAccess, IInvoiceService invoiceService)
    {
        _context = context;
        _vehicleStatusService = vehicleStatusService;
        _branchAccess = branchAccess;
        _invoiceService = invoiceService;
    }

    #region Contract List for Handover

    public async Task<PagedResult<ContractForHandoverListDto>> GetContractsForHandoverAsync(HandoverFilterDto filter)
    {
        // Luồng mới: Lấy cả Draft, PendingSigning (chưa giao xe hoặc đang chờ thanh toán)
        var query = _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
                    .ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Branch)
            .Include(c => c.HandoverRecord)
            .Where(c => c.Status == RentalContractStatus.Draft ||
                        c.Status == RentalContractStatus.PendingSigning)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.PlannedStart)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => MapToContractListDto(c))
            .ToListAsync();

        return new PagedResult<ContractForHandoverListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<ContractForHandoverListDto>> GetContractsForReturnAsync(HandoverFilterDto filter)
    {
        // Luồng mới: Active = đang thuê, InProgress = đang trả xe
        var query = _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
                    .ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Branch)
            .Include(c => c.HandoverRecord)
            .Include(c => c.ReturnRecord)
            .Where(c => c.Status == RentalContractStatus.Active ||
                        c.Status == RentalContractStatus.InProgress)
            .Where(c => c.HandoverRecord != null && c.ReturnRecord == null)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        if (filter.IsOverdue == true)
        {
            query = query.Where(c => c.PlannedEnd < DateTime.UtcNow);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.PlannedEnd)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => MapToContractListDto(c))
            .ToListAsync();

        return new PagedResult<ContractForHandoverListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<ContractForHandoverListDto>> GetOverdueContractsAsync()
    {
        var query = _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
                    .ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Branch)
            .Where(c => c.Status == RentalContractStatus.InProgress)
            .Where(c => c.PlannedEnd < DateTime.UtcNow)
            .Where(c => c.ReturnRecord == null)
            .AsQueryable();

        // Branch filtering
        var userBranchId = _branchAccess.GetCurrentUserBranchId();
        if (userBranchId.HasValue)
        {
            query = query.Where(c => c.Vehicle.BranchId == userBranchId.Value);
        }

        var contracts = await query
            .OrderBy(c => c.PlannedEnd)
            .Take(50)
            .ToListAsync();

        return contracts.Select(c => MapToContractListDto(c));
    }


    #endregion

    #region Check-Out (Handover)

    public async Task<CheckOutFormDto?> GetCheckOutFormAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
                    .ThenInclude(m => m.VehicleType)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Branch)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return null;

        return new CheckOutFormDto
        {
            ContractId = contract.ContractId,
            ContractCode = $"HD-{contract.ContractId.ToString()[..8].ToUpper()}",
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            CustomerIdNumber = "", // No IdentityNumber in model
            VehicleId = contract.VehicleId,
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            CurrentOdoKm = contract.Vehicle.CurrentOdoKm,
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            DepositAmount = contract.SnapshotDepositAmount,
            DepositPaid = false, // Luồng mới: Thanh toán tại quầy khi giao xe
            RentalAmount = contract.SnapshotUnitPrice,
            BranchName = contract.Vehicle.Branch.Name,
            BranchAddress = contract.Vehicle.Branch.Address ?? "",
            DefaultAccessories = GetDefaultAccessories()
        };
    }

    /// <summary>
    /// Kiểm tra hợp đồng có thể giao xe không - Luồng mới
    /// Cho phép giao xe khi hợp đồng ở trạng thái Draft hoặc PendingSigning
    /// </summary>
    public async Task<ServiceResult> CanCheckOutAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.HandoverRecord)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult.Fail("Hợp đồng không tồn tại");

        if (contract.HandoverRecord != null)
            return ServiceResult.Fail("Hợp đồng đã được giao xe");

        // Luồng mới: Cho phép giao xe khi Draft hoặc PendingSigning
        // Draft: Hợp đồng nháp, chưa cập nhật phụ kiện
        // PendingSigning: Đã lập biên bản, chờ ký và thanh toán
        var allowedStatuses = new[] 
        { 
            RentalContractStatus.Draft, 
            RentalContractStatus.PendingSigning,
            RentalContractStatus.Active // Giữ để tương thích ngược
        };
        
        if (!allowedStatuses.Contains(contract.Status))
            return ServiceResult.Fail($"Hợp đồng không ở trạng thái cho phép giao xe (Trạng thái hiện tại: {contract.Status})");

        if (contract.Vehicle.CurrentStatus != VehicleStatus.Available &&
            contract.Vehicle.CurrentStatus != VehicleStatus.Reserved)
            return ServiceResult.Fail($"Xe đang ở trạng thái '{VehicleStatusHelper.GetDisplayName(contract.Vehicle.CurrentStatus)}', không thể giao");

        // Luồng mới: Không kiểm tra đặt cọc vì thanh toán tại quầy sau khi ký
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<Guid>> ConfirmCheckOutAsync(CheckOutDto dto, Guid userId)
    {
        var canCheckOut = await CanCheckOutAsync(dto.ContractId);
        if (!canCheckOut.Success)
            return ServiceResult<Guid>.Fail(canCheckOut.Errors.First());

        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .FirstAsync(c => c.ContractId == dto.ContractId);

        var handover = new HandoverRecord
        {
            HandoverId = Guid.NewGuid(),
            ContractId = dto.ContractId,
            OdoKmOut = dto.OdoKmOut,
            FuelLevelOut = dto.FuelLevelOut,
            HandedAt = DateTime.UtcNow,
            ExteriorCondition = dto.ExteriorCondition,
            InteriorCondition = dto.InteriorCondition,
            PreExistingDamages = dto.PreExistingDamages,
            VehicleConditionImgRef = dto.VehicleConditionImages,
            CustomerConfirmed = dto.CustomerConfirmed,
            CustomerConfirmedAt = dto.CustomerConfirmed ? DateTime.UtcNow : null,
            HandedOverBy = userId,
            Note = dto.Note
        };

        _context.HandoverRecords.Add(handover);

        foreach (var acc in dto.Accessories.Where(a => a.IsChecked))
        {
            _context.HandoverAccessories.Add(new HandoverAccessory
            {
                AccessoryId = Guid.NewGuid(),
                HandoverId = handover.HandoverId,
                AccessoryName = acc.AccessoryName,
                Quantity = acc.Quantity,
                EstimatedValue = acc.EstimatedValue,
                IsReturnedOk = true
            });
        }

        // Luồng mới: Sau khi lập biên bản, chuyển sang PendingSigning để chờ ký + thanh toán
        // Nếu đã thanh toán và ký rồi thì mới chuyển sang InProgress
        contract.Status = RentalContractStatus.PendingSigning;
        contract.Vehicle.CurrentOdoKm = dto.OdoKmOut;

        _context.VehicleStatusHistories.Add(new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = contract.VehicleId,
            FromStatus = contract.Vehicle.CurrentStatus.ToString(),
            ToStatus = VehicleStatus.Reserved.ToString(),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = userId,
            Note = $"Lập biên bản giao xe - HD-{dto.ContractId.ToString()[..8].ToUpper()}"
        });

        contract.Vehicle.CurrentStatus = VehicleStatus.Reserved;

        await _context.SaveChangesAsync();

        // Luồng mới: KHÔNG tạo invoice tự động - thanh toán được xử lý riêng sau khi ký
        // Hóa đơn giao xe được tạo thủ công bởi nhân viên

        return ServiceResult<Guid>.Ok(handover.HandoverId, "Lập biên bản giao xe thành công. Vui lòng in hợp đồng và biên bản để khách ký.");
    }

    /// <summary>
    /// Xác nhận hoàn tất giao xe - Luồng mới
    /// Gọi sau khi khách đã ký hợp đồng và thanh toán
    /// </summary>
    public async Task<ServiceResult> CompleteHandoverAsync(Guid contractId, Guid staffId, Guid? paymentTxnId = null)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.HandoverRecord)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult.Fail("Hợp đồng không tồn tại");

        if (contract.HandoverRecord == null)
            return ServiceResult.Fail("Chưa lập biên bản giao xe");

        if (contract.Status != RentalContractStatus.PendingSigning)
            return ServiceResult.Fail($"Hợp đồng không ở trạng thái chờ ký (Trạng thái hiện tại: {contract.Status})");

        // Kiểm tra đã thanh toán chưa
        if (paymentTxnId.HasValue)
        {
            var payment = await _context.PaymentTransactions.FindAsync(paymentTxnId.Value);
            if (payment == null || payment.Status != TransactionStatus.Success)
                return ServiceResult.Fail("Thanh toán chưa được xác nhận");
        }

        // Cập nhật hợp đồng
        contract.Status = RentalContractStatus.Active;
        contract.CustomerSigned = true;
        contract.CustomerSignedAt = DateTime.UtcNow;
        contract.ConfirmedBy = staffId;
        contract.ConfirmedAt = DateTime.UtcNow;
        contract.ActualStart = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        // Cập nhật biên bản
        contract.HandoverRecord.CustomerConfirmed = true;
        contract.HandoverRecord.CustomerConfirmedAt = DateTime.UtcNow;

        // Cập nhật xe
        contract.Vehicle.CurrentStatus = VehicleStatus.Renting;

        _context.VehicleStatusHistories.Add(new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = contract.VehicleId,
            FromStatus = VehicleStatus.Reserved.ToString(),
            ToStatus = VehicleStatus.Renting.ToString(),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = staffId,
            Note = $"Hoàn tất giao xe - HD-{contractId.ToString()[..8].ToUpper()}"
        });

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Hoàn tất giao xe thành công. Khách hàng đã nhận xe.");
    }

    #endregion

    #region Check-In (Return)

    public async Task<CheckInFormDto?> GetCheckInFormAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Branch)
            .Include(c => c.HandoverRecord)
                .ThenInclude(h => h!.Accessories)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract?.HandoverRecord == null) return null;

        var handover = contract.HandoverRecord;

        return new CheckInFormDto
        {
            ContractId = contract.ContractId,
            ContractCode = $"HD-{contract.ContractId.ToString()[..8].ToUpper()}",
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            VehicleId = contract.VehicleId,
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            OdoKmOut = handover.OdoKmOut,
            FuelLevelOut = handover.FuelLevelOut,
            HandedAt = handover.HandedAt,
            PreExistingDamages = handover.PreExistingDamages,
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            HandedAccessories = handover.Accessories.Select(a => new AccessoryReturnDto
            {
                AccessoryName = a.AccessoryName,
                QuantityOut = a.Quantity,
                QuantityIn = a.Quantity,
                IsReturnedOk = true,
                EstimatedValue = a.EstimatedValue
            }).ToList(),
            BranchName = contract.Vehicle.Branch.Name
        };
    }

    public async Task<ServiceResult> CanCheckInAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.HandoverRecord)
            .Include(c => c.ReturnRecord)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult.Fail("Hợp đồng không tồn tại");

        if (contract.HandoverRecord == null)
            return ServiceResult.Fail("Hợp đồng chưa được giao xe");

        if (contract.ReturnRecord != null)
            return ServiceResult.Fail("Hợp đồng đã được nhận xe");

        // Luồng mới: Active = đang thuê, InProgress = đang xử lý trả xe
        if (contract.Status != RentalContractStatus.Active &&
            contract.Status != RentalContractStatus.InProgress)
            return ServiceResult.Fail("Hợp đồng không ở trạng thái cho phép nhận xe");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<Guid>> ConfirmCheckInAsync(CheckInDto dto, Guid userId)
    {
        var canCheckIn = await CanCheckInAsync(dto.ContractId);
        if (!canCheckIn.Success)
            return ServiceResult<Guid>.Fail(canCheckIn.Errors.First());

        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.HandoverRecord)
            .FirstAsync(c => c.ContractId == dto.ContractId);

        var handover = contract.HandoverRecord!;

        if (dto.OdoKmIn < handover.OdoKmOut)
            return ServiceResult<Guid>.Fail($"Số km khi nhận ({dto.OdoKmIn:N0}) không được nhỏ hơn số km khi giao ({handover.OdoKmOut:N0})");

        var overtimeHours = 0m;
        if (DateTime.UtcNow > contract.PlannedEnd)
        {
            overtimeHours = (decimal)(DateTime.UtcNow - contract.PlannedEnd).TotalHours;
        }

        var fuelShortage = Math.Max(0, handover.FuelLevelOut - dto.FuelLevelIn);

        var returnRecord = new ReturnRecord
        {
            ReturnId = Guid.NewGuid(),
            ContractId = dto.ContractId,
            OdoKmIn = dto.OdoKmIn,
            FuelLevelIn = dto.FuelLevelIn,
            ReturnedAt = DateTime.UtcNow,
            ExteriorCondition = dto.ExteriorCondition,
            InteriorCondition = dto.InteriorCondition,
            DamagesFound = dto.DamagesFound,
            VehicleConditionImgRef = dto.VehicleConditionImages,
            NeedsCleaning = dto.NeedsCleaning,
            NeedsMaintenance = dto.NeedsMaintenance,
            OvertimeHours = overtimeHours,
            FuelShortage = fuelShortage,
            ReceivedBy = userId,
            Note = dto.Note
        };

        _context.ReturnRecords.Add(returnRecord);

        foreach (var acc in dto.AccessoriesReturned)
        {
            _context.HandoverAccessories.Add(new HandoverAccessory
            {
                AccessoryId = Guid.NewGuid(),
                ReturnId = returnRecord.ReturnId,
                AccessoryName = acc.AccessoryName,
                Quantity = acc.QuantityIn,
                IsReturnedOk = acc.IsReturnedOk,
                DamageNote = acc.DamageNote,
                EstimatedValue = acc.EstimatedValue
            });
        }

        foreach (var charge in dto.AdditionalCharges.Where(c => c.Amount > 0))
        {
            _context.ContractCharges.Add(new ContractCharge
            {
                ChargeId = Guid.NewGuid(),
                ContractId = dto.ContractId,
                ChargeType = charge.ChargeType,
                Amount = charge.Amount,
                Description = charge.Description,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (fuelShortage > 0)
        {
            var fuelCharge = fuelShortage * 5000; // 5,000đ/% nhiên liệu thiếu
            _context.ContractCharges.Add(new ContractCharge
            {
                ChargeId = Guid.NewGuid(),
                ContractId = dto.ContractId,
                ChargeType = ChargeType.FuelShortage,
                Amount = fuelCharge,
                Description = $"Nhiên liệu thiếu {fuelShortage:N0}%",
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // === TÍNH PHÍ TRẢ TRỄ THEO LUỒNG MỚI ===
        // - Miễn phí nếu trễ < 3 giờ
        // - Trễ >= 3 giờ và < 12 giờ: tính theo giờ
        // - Trễ >= 12 giờ: tính theo ngày
        if (overtimeHours >= 3)
        {
            decimal overtimeCharge;
            string description;
            var hourlyRate = contract.SnapshotOvertimeHourlyPrice > 0 
                ? contract.SnapshotOvertimeHourlyPrice 
                : 50000m; // Default 50,000đ/giờ
            var dailyRate = contract.SnapshotBaseDailyPrice > 0 
                ? contract.SnapshotBaseDailyPrice 
                : 1000000m; // Default 1,000,000đ/ngày

            if (overtimeHours >= 12)
            {
                // Tính theo ngày (làm tròn lên)
                var overtimeDays = Math.Ceiling(overtimeHours / 24m);
                overtimeCharge = overtimeDays * dailyRate;
                description = $"Trả trễ {overtimeHours:N1} giờ ({overtimeDays:N0} ngày) x {dailyRate:N0}đ";
            }
            else
            {
                // Tính theo giờ (làm tròn lên, từ giờ thứ 3)
                var chargeableHours = Math.Ceiling(overtimeHours);
                overtimeCharge = chargeableHours * hourlyRate;
                description = $"Trả trễ {chargeableHours:N0} giờ x {hourlyRate:N0}đ";
            }

            _context.ContractCharges.Add(new ContractCharge
            {
                ChargeId = Guid.NewGuid(),
                ContractId = dto.ContractId,
                ChargeType = ChargeType.OvertimeFee,
                Amount = overtimeCharge,
                Description = description,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        var nextVehicleStatus = VehicleStatus.Available;
        if (dto.NeedsMaintenance || !string.IsNullOrEmpty(dto.DamagesFound))
        {
            nextVehicleStatus = VehicleStatus.Maintenance;
        }

        contract.Status = RentalContractStatus.PendingSettlement;
        contract.ActualEnd = DateTime.UtcNow;
        contract.Vehicle.CurrentStatus = nextVehicleStatus;
        contract.Vehicle.CurrentOdoKm = dto.OdoKmIn;

        _context.VehicleStatusHistories.Add(new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = contract.VehicleId,
            FromStatus = VehicleStatus.Renting.ToString(),
            ToStatus = nextVehicleStatus.ToString(),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = userId,
            Note = $"Nhận xe theo hợp đồng HD-{dto.ContractId.ToString()[..8].ToUpper()}"
        });

        await _context.SaveChangesAsync();

        // === LUỒNG MỚI: Tạo Invoice sau khi nhận xe ===
        try
        {
            // 1. Tạo Surcharge/Penalty Invoice nếu có phí phát sinh
            var hasCharges = await _context.ContractCharges
                .AnyAsync(c => c.ContractId == dto.ContractId && !c.IsPaid);
            
            if (hasCharges)
            {
                var surchargeInvoiceId = await _invoiceService.CreateSurchargePenaltyInvoiceAsync(dto.ContractId, userId);
                if (surchargeInvoiceId.HasValue && surchargeInvoiceId.Value != Guid.Empty)
                {
                    Console.WriteLine($"Surcharge Invoice created: {surchargeInvoiceId.Value} for contract {dto.ContractId}");
                }
            }

            // 2. Tạo Return Invoice (hoàn cọc) - LUÔN tạo sau khi nhận xe
            var returnInvoiceId = await _invoiceService.CreateReturnInvoiceAsync(dto.ContractId, userId);
            Console.WriteLine($"Return Invoice created: {returnInvoiceId} for contract {dto.ContractId}");
        }
        catch (Exception ex)
        {
            // Don't fail check-in if invoice creation fails
            Console.WriteLine($"Error creating invoices for contract {dto.ContractId}: {ex.Message}");
        }

        return ServiceResult<Guid>.Ok(returnRecord.ReturnId, "Nhận xe thành công. Vui lòng tiến hành thanh toán/hoàn tiền.");
    }

    /// <summary>
    /// Hoàn tất trả xe - Luồng mới
    /// Gọi sau khi khách thanh toán/hoàn tiền xong
    /// </summary>
    public async Task<ServiceResult> CompleteReturnAsync(Guid contractId, Guid staffId, Guid? paymentTxnId = null)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.ReturnRecord)
            .Include(c => c.Charges)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return ServiceResult.Fail("Hợp đồng không tồn tại");

        if (contract.ReturnRecord == null)
            return ServiceResult.Fail("Chưa lập biên bản nhận xe");

        if (contract.Status != RentalContractStatus.PendingSettlement)
            return ServiceResult.Fail($"Hợp đồng không ở trạng thái chờ quyết toán (Trạng thái hiện tại: {contract.Status})");

        // Kiểm tra thanh toán nếu có paymentTxnId
        if (paymentTxnId.HasValue)
        {
            var payment = await _context.PaymentTransactions.FindAsync(paymentTxnId.Value);
            if (payment == null || payment.Status != TransactionStatus.Success)
                return ServiceResult.Fail("Thanh toán chưa được xác nhận");
        }

        // Return Invoice đã được tạo khi CheckIn (ConfirmCheckInAsync)
        // Không cần tạo lại ở đây

        // Đánh dấu tất cả charges đã thanh toán
        foreach (var charge in contract.Charges.Where(c => !c.IsPaid))
        {
            charge.IsPaid = true;
        }

        // Cập nhật hợp đồng
        contract.Status = RentalContractStatus.Completed;
        contract.ReturnRecord.CustomerConfirmed = true;
        contract.ReturnRecord.CustomerConfirmedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Hoàn tất trả xe thành công. Hợp đồng đã kết thúc.");
    }

    #endregion

    #region Documents

    public async Task<HandoverRecordDetailDto?> GetHandoverRecordAsync(Guid contractId)
    {
        var handover = await _context.HandoverRecords
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Customer)
                    .ThenInclude(cu => cu.UserAccount)
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
                        .ThenInclude(m => m.VehicleType)
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Branch)
            .Include(h => h.HandedOverByUser)
            .Include(h => h.Accessories)
            .FirstOrDefaultAsync(h => h.ContractId == contractId);

        if (handover == null) return null;

        var contract = handover.RentalContract;

        return new HandoverRecordDetailDto
        {
            HandoverId = handover.HandoverId,
            ContractId = contract.ContractId,
            ContractCode = $"HD-{contract.ContractId.ToString()[..8].ToUpper()}",
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            CustomerIdNumber = "",
            CustomerAddress = contract.Customer.AddressText ?? "",
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            VehicleTypeName = contract.Vehicle.Model.VehicleType.TypeName,
            Color = contract.Vehicle.Color ?? "",
            OdoKmOut = handover.OdoKmOut,
            FuelLevelOut = handover.FuelLevelOut,
            HandedAt = handover.HandedAt,
            ExteriorCondition = handover.ExteriorCondition,
            InteriorCondition = handover.InteriorCondition,
            PreExistingDamages = handover.PreExistingDamages,
            VehicleImages = ParseImagePaths(handover.VehicleConditionImgRef),
            Accessories = handover.Accessories.Select(a => new AccessoryDto
            {
                AccessoryName = a.AccessoryName,
                Quantity = a.Quantity,
                EstimatedValue = a.EstimatedValue,
                IsChecked = true
            }).ToList(),
            CustomerConfirmed = handover.CustomerConfirmed,
            CustomerConfirmedAt = handover.CustomerConfirmedAt,
            Note = handover.Note,
            HandedOverByName = handover.HandedOverByUser.StaffProfile?.FullName ?? handover.HandedOverByUser.Username,
            BranchName = contract.Vehicle.Branch.Name,
            BranchAddress = contract.Vehicle.Branch.Address ?? "",
            BranchPhone = contract.Vehicle.Branch.PhoneContact ?? "",
            PlannedStart = contract.PlannedStart,
            PlannedEnd = contract.PlannedEnd,
            DepositAmount = contract.SnapshotDepositAmount,
            RentalAmount = contract.SnapshotUnitPrice
        };
    }

    public async Task<ReturnRecordDetailDto?> GetReturnRecordAsync(Guid contractId)
    {
        var returnRecord = await _context.ReturnRecords
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Customer)
                    .ThenInclude(cu => cu.UserAccount)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Branch)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.HandoverRecord)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Charges)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.PaymentTransactions)
            .Include(r => r.ReceivedByUser)
            .Include(r => r.AccessoriesReturned)
            .FirstOrDefaultAsync(r => r.ContractId == contractId);

        if (returnRecord == null) return null;

        var contract = returnRecord.RentalContract;
        var handover = contract.HandoverRecord!;
        
        // Calculate payment info
        var totalPaid = contract.PaymentTransactions
            .Where(p => p.Status == Models.Enums.TransactionStatus.Success)
            .Sum(p => p.Amount);

        return new ReturnRecordDetailDto
        {
            ReturnId = returnRecord.ReturnId,
            ContractId = contract.ContractId,
            ContractCode = $"HD-{contract.ContractId.ToString()[..8].ToUpper()}",
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            OdoKmOut = handover.OdoKmOut,
            OdoKmIn = returnRecord.OdoKmIn,
            FuelLevelOut = handover.FuelLevelOut,
            FuelLevelIn = returnRecord.FuelLevelIn,
            FuelShortage = returnRecord.FuelShortage,
            HandedAt = handover.HandedAt,
            ReturnedAt = returnRecord.ReturnedAt,
            OvertimeHours = returnRecord.OvertimeHours,
            ExteriorCondition = returnRecord.ExteriorCondition,
            InteriorCondition = returnRecord.InteriorCondition,
            DamagesFound = returnRecord.DamagesFound,
            VehicleImages = ParseImagePaths(returnRecord.VehicleConditionImgRef),
            NeedsCleaning = returnRecord.NeedsCleaning,
            NeedsMaintenance = returnRecord.NeedsMaintenance,
            AccessoriesReturned = returnRecord.AccessoriesReturned.Select(a => new AccessoryReturnDto
            {
                AccessoryName = a.AccessoryName,
                QuantityOut = a.Quantity,
                QuantityIn = a.Quantity,
                IsReturnedOk = a.IsReturnedOk,
                DamageNote = a.DamageNote,
                EstimatedValue = a.EstimatedValue
            }).ToList(),
            AdditionalCharges = contract.Charges.Select(c => new ChargeDto
            {
                ChargeType = c.ChargeType,
                Amount = c.Amount,
                Description = c.Description
            }).ToList(),
            RentalDays = contract.RentalDays,
            RentalUnitPrice = contract.SnapshotUnitPrice,
            RentalAmount = contract.RentalAmount,
            // === DEPOSIT INFO ===
            ResponsibilityDeposit = contract.ResponsibilityDeposit,
            RentalDeposit = contract.RentalDeposit,
            DepositAmount = contract.ResponsibilityDeposit + contract.RentalDeposit, // Tổng cọc
            AmountPaid = totalPaid,
            IsPaid = contract.Status == Models.Enums.RentalContractStatus.Completed,
            Note = returnRecord.Note,
            ReceivedByName = returnRecord.ReceivedByUser.StaffProfile?.FullName ?? returnRecord.ReceivedByUser.Username,
            BranchName = contract.Vehicle.Branch.Name
        };
    }

    public async Task<PagedResult<HandoverDocumentListDto>> GetDocumentsAsync(HandoverDocumentFilterDto filter)
    {
        var handovers = _context.HandoverRecords
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Customer)
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Branch)
            .Include(h => h.HandedOverByUser)
            .Select(h => new HandoverDocumentListDto
            {
                DocumentId = h.HandoverId,
                DocumentType = "Handover",
                ContractId = h.ContractId,
                ContractCode = "HD-" + h.ContractId.ToString().Substring(0, 8).ToUpper(),
                CustomerName = h.RentalContract.Customer.FullName,
                PlateNo = h.RentalContract.Vehicle.PlateNo,
                VehicleName = h.RentalContract.Vehicle.Model.Make + " " + h.RentalContract.Vehicle.Model.ModelName,
                DocumentDate = h.HandedAt,
                StaffName = h.HandedOverByUser.StaffProfile != null ? h.HandedOverByUser.StaffProfile.FullName : h.HandedOverByUser.Username,
                BranchName = h.RentalContract.Vehicle.Branch.Name
            });

        var returns = _context.ReturnRecords
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Customer)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Branch)
            .Include(r => r.ReceivedByUser)
            .Select(r => new HandoverDocumentListDto
            {
                DocumentId = r.ReturnId,
                DocumentType = "Return",
                ContractId = r.ContractId,
                ContractCode = "HD-" + r.ContractId.ToString().Substring(0, 8).ToUpper(),
                CustomerName = r.RentalContract.Customer.FullName,
                PlateNo = r.RentalContract.Vehicle.PlateNo,
                VehicleName = r.RentalContract.Vehicle.Model.Make + " " + r.RentalContract.Vehicle.Model.ModelName,
                DocumentDate = r.ReturnedAt,
                StaffName = r.ReceivedByUser.StaffProfile != null ? r.ReceivedByUser.StaffProfile.FullName : r.ReceivedByUser.Username,
                BranchName = r.RentalContract.Vehicle.Branch.Name
            });

        IQueryable<HandoverDocumentListDto> query;

        if (filter.DocumentType == "Handover")
            query = handovers;
        else if (filter.DocumentType == "Return")
            query = returns;
        else
            query = handovers.Union(returns);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.PlateNo.ToLower().Contains(term) ||
                                    d.CustomerName.ToLower().Contains(term) ||
                                    d.ContractCode.ToLower().Contains(term));
        }

        if (filter.FromDate.HasValue)
            query = query.Where(d => d.DocumentDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(d => d.DocumentDate <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.DocumentDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<HandoverDocumentListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion

    #region Private Helpers

    private IQueryable<RentalContract> ApplyFilters(IQueryable<RentalContract> query, HandoverFilterDto filter)
    {
        // *** BRANCH ACCESS FILTER ***
        // BranchManager và Staff chỉ thấy hợp đồng của xe thuộc chi nhánh mình
        var userBranchId = _branchAccess.GetCurrentUserBranchId();
        if (userBranchId.HasValue)
        {
            query = query.Where(c => c.Vehicle.BranchId == userBranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(c => c.Vehicle.PlateNo.ToLower().Contains(term) ||
                                    c.Customer.FullName.ToLower().Contains(term) ||
                                    c.Customer.UserAccount.Phone!.Contains(term));
        }

        // Chỉ filter thêm theo BranchId nếu Admin muốn lọc theo chi nhánh cụ thể
        if (filter.BranchId.HasValue && !userBranchId.HasValue)
            query = query.Where(c => c.Vehicle.BranchId == filter.BranchId.Value);

        if (filter.Status.HasValue)
            query = query.Where(c => c.Status == filter.Status.Value);

        // Validate date range - only apply if FromDate <= ToDate
        if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            // If FromDate > ToDate, skip date filters (invalid range)
            if (filter.FromDate.Value <= filter.ToDate.Value)
            {
                query = query.Where(c => c.PlannedStart >= filter.FromDate.Value &&
                                        c.PlannedStart <= filter.ToDate.Value);
            }
        }
        else
        {
            // Apply individual date filters if only one is provided
            if (filter.FromDate.HasValue)
                query = query.Where(c => c.PlannedStart >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(c => c.PlannedStart <= filter.ToDate.Value);
        }

        return query;
    }

    private static ContractForHandoverListDto MapToContractListDto(RentalContract c)
    {
        return new ContractForHandoverListDto
        {
            ContractId = c.ContractId,
            CustomerId = c.CustomerId,
            CustomerName = c.Customer?.FullName ?? "Unknown",
            CustomerPhone = c.Customer?.UserAccount?.Phone ?? "",
            VehicleId = c.VehicleId,
            PlateNo = c.Vehicle?.PlateNo ?? "Unknown",
            VehicleName = c.Vehicle?.Model != null ? $"{c.Vehicle.Model.Make} {c.Vehicle.Model.ModelName}" : "Unknown",
            VehicleTypeName = c.Vehicle?.Model?.VehicleType?.TypeName ?? "Unknown",
            BranchId = c.Vehicle?.BranchId ?? Guid.Empty,
            BranchName = c.Vehicle?.Branch?.Name ?? "Unknown",
            PlannedStart = c.PlannedStart,
            PlannedEnd = c.PlannedEnd,
            Status = c.Status,
            HasHandoverRecord = c.HandoverRecord != null,
            HasReturnRecord = c.ReturnRecord != null
        };
    }

    private static List<AccessoryDto> GetDefaultAccessories()
    {
        return new List<AccessoryDto>
        {
            new() { AccessoryName = "Chìa khóa xe", Quantity = 2, EstimatedValue = 5000000, IsChecked = true },
            new() { AccessoryName = "Cavet xe (bản photo)", Quantity = 1, EstimatedValue = 0, IsChecked = true },
            new() { AccessoryName = "Giấy đăng kiểm", Quantity = 1, EstimatedValue = 0, IsChecked = true },
            new() { AccessoryName = "Bảo hiểm xe", Quantity = 1, EstimatedValue = 0, IsChecked = true },
            new() { AccessoryName = "Thẻ gửi xe", Quantity = 2, EstimatedValue = 100000, IsChecked = true },
            new() { AccessoryName = "Bộ dụng cụ sửa chữa", Quantity = 1, EstimatedValue = 500000, IsChecked = true },
            new() { AccessoryName = "Lốp sơ cua", Quantity = 1, EstimatedValue = 2000000, IsChecked = true },
            new() { AccessoryName = "Camera hành trình", Quantity = 1, EstimatedValue = 1500000, IsChecked = false }
        };
    }

    private static List<string> ParseImagePaths(string? imagesJson)
    {
        if (string.IsNullOrEmpty(imagesJson)) return new List<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    #endregion

    #region Incidents (D13)

    public async Task<IEnumerable<IncidentDto>> GetAllIncidentsAsync()
    {
        var incidents = await _context.Incidents
            .Include(i => i.Vehicle)
                .ThenInclude(v => v.Model)
            .Include(i => i.RentalContract)
            .Include(i => i.Customer)
            .Include(i => i.FineDetail)
            .Include(i => i.ImpoundDetail)
            .OrderByDescending(i => i.OccurredAt)
            .ToListAsync();

        return incidents.Select(i => new IncidentDto
        {
            IncidentId = i.IncidentId,
            IncidentType = i.IncidentType,
            VehicleId = i.VehicleId,
            PlateNo = i.Vehicle?.PlateNo ?? "",
            VehicleName = i.Vehicle?.Model != null ? $"{i.Vehicle.Model.Make} {i.Vehicle.Model.ModelName}" : "",
            ContractId = i.ContractId,
            ContractCode = "HD-" + i.ContractId.ToString().Substring(0, 8).ToUpper(),
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.FullName,
            OccurredAt = i.OccurredAt,
            Location = i.Location,
            Description = i.Description,
            Status = i.Status,
            EstimatedCost = i.EstimatedCost,
            FineDetail = i.FineDetail != null ? new FineDetailDto
            {
                TicketNumber = i.FineDetail.TicketNumber,
                AgencyName = i.FineDetail.AgencyName,
                DueDate = i.FineDetail.DueDate,
                FineAmount = i.FineDetail.FineAmount
            } : null,
            ImpoundDetail = i.ImpoundDetail != null ? new ImpoundDetailDto
            {
                ImpoundLotAddress = i.ImpoundDetail.ImpoundLotAddress,
                ImpoundedAt = i.ImpoundDetail.ImpoundedAt,
                ReleasedAt = i.ImpoundDetail.ReleasedAt
            } : null
        });
    }

    public async Task<IEnumerable<IncidentDto>> GetIncidentsByContractAsync(Guid contractId)
    {
        var incidents = await _context.Incidents
            .Include(i => i.Vehicle)
                .ThenInclude(v => v.Model)
            .Include(i => i.RentalContract)
            .Include(i => i.Customer)
            .Include(i => i.FineDetail)
            .Include(i => i.ImpoundDetail)
            .Where(i => i.ContractId == contractId)
            .OrderByDescending(i => i.OccurredAt)
            .ToListAsync();

        return incidents.Select(i => new IncidentDto
        {
            IncidentId = i.IncidentId,
            IncidentType = i.IncidentType,
            VehicleId = i.VehicleId,
            PlateNo = i.Vehicle?.PlateNo ?? "",
            VehicleName = i.Vehicle?.Model != null ? $"{i.Vehicle.Model.Make} {i.Vehicle.Model.ModelName}" : "",
            ContractId = i.ContractId,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.FullName,
            OccurredAt = i.OccurredAt,
            Location = i.Location,
            Description = i.Description,
            Status = i.Status,
            EstimatedCost = i.EstimatedCost,
            FineDetail = i.FineDetail != null ? new FineDetailDto
            {
                TicketNumber = i.FineDetail.TicketNumber,
                AgencyName = i.FineDetail.AgencyName,
                DueDate = i.FineDetail.DueDate,
                FineAmount = i.FineDetail.FineAmount
            } : null,
            ImpoundDetail = i.ImpoundDetail != null ? new ImpoundDetailDto
            {
                ImpoundLotAddress = i.ImpoundDetail.ImpoundLotAddress,
                ImpoundedAt = i.ImpoundDetail.ImpoundedAt,
                ReleasedAt = i.ImpoundDetail.ReleasedAt
            } : null
        });
    }

    public async Task<ServiceResult<Guid>> CreateIncidentAsync(IncidentCreateDto dto, Guid userId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Vehicle)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == dto.ContractId);

        if (contract == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hợp đồng");

        if (!dto.IncidentType.HasValue)
            return ServiceResult<Guid>.Fail("Vui lòng chọn loại sự cố");

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentType = dto.IncidentType.Value,
            VehicleId = contract.VehicleId,
            ContractId = dto.ContractId,
            CustomerId = contract.CustomerId,
            OccurredAt = dto.OccurredAt,
            Location = dto.Location,
            Description = dto.Description,
            Status = IncidentStatus.New,
            EstimatedCost = dto.EstimatedCost,
            CreatedBy = userId
        };

        _context.Incidents.Add(incident);
        
        // Thêm chi tiết phạt nguội nếu là Fine
        if (dto.IncidentType == IncidentType.Fine && dto.FineDetail != null)
        {
            var fineDetail = new IncidentFineDetail
            {
                IncidentId = incident.IncidentId,
                TicketNumber = dto.FineDetail.TicketNumber,
                AgencyName = dto.FineDetail.AgencyName,
                DueDate = dto.FineDetail.DueDate,
                FineAmount = dto.FineDetail.FineAmount ?? 0
            };
            _context.Set<IncidentFineDetail>().Add(fineDetail);
            
            // Cập nhật EstimatedCost từ FineAmount nếu chưa có
            if (incident.EstimatedCost == 0 && dto.FineDetail.FineAmount.HasValue)
                incident.EstimatedCost = dto.FineDetail.FineAmount.Value;
        }
        
        // Thêm chi tiết tạm giữ xe nếu là Impound
        if (dto.IncidentType == IncidentType.Impound && dto.ImpoundDetail != null)
        {
            var impoundDetail = new IncidentImpoundDetail
            {
                IncidentId = incident.IncidentId,
                ImpoundLotAddress = dto.ImpoundDetail.ImpoundLotAddress,
                ImpoundedAt = dto.ImpoundDetail.ImpoundedAt,
                ReleasedAt = dto.ImpoundDetail.ReleasedAt
            };
            _context.Set<IncidentImpoundDetail>().Add(impoundDetail);
        }
        
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(incident.IncidentId, "Đã ghi nhận sự cố thành công");
    }

    #endregion

    #region Violations (D13)

    public async Task<IEnumerable<ViolationDto>> GetViolationsByContractAsync(Guid contractId)
    {
        var violations = await _context.ContractViolations
            .Include(v => v.RentalContract)
            .Where(v => v.ContractId == contractId)
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync();

        return violations.Select(v => new ViolationDto
        {
            ViolationId = v.ViolationId,
            ContractId = v.ContractId,
            ViolationType = v.ViolationType,
            DetectedAt = v.DetectedAt,
            Description = v.Description,
            Status = v.Status,
            PenaltyAmount = v.PenaltyAmount
        });
    }

    public async Task<ServiceResult<Guid>> CreateViolationAsync(ViolationCreateDto dto, Guid userId)
    {
        var contract = await _context.RentalContracts
            .FirstOrDefaultAsync(c => c.ContractId == dto.ContractId);

        if (contract == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hợp đồng");

        var violation = new ContractViolation
        {
            ViolationId = Guid.NewGuid(),
            ContractId = dto.ContractId,
            ViolationType = dto.ViolationType,
            DetectedAt = DateTime.UtcNow,
            Description = dto.Description,
            Status = ViolationStatus.Open,
            PenaltyAmount = dto.PenaltyAmount,
            CreatedBy = userId
        };

        _context.ContractViolations.Add(violation);

        // Tự động tạo ContractCharge cho mức phạt
        if (dto.PenaltyAmount > 0)
        {
            var charge = new ContractCharge
            {
                ChargeId = Guid.NewGuid(),
                ContractId = dto.ContractId,
                ChargeType = ChargeType.DamageFee,
                Amount = dto.PenaltyAmount,
                Description = $"Phạt vi phạm: {dto.Description}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ContractCharges.Add(charge);
        }

        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(violation.ViolationId, "Đã ghi nhận vi phạm thành công");
    }

    #endregion
}
