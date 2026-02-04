using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;
using UCar.ViewModels.Payment;

namespace UCar.Services;

/// <summary>
/// Service quản lý thanh toán
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IInvoiceService _invoiceService;
    private readonly IHandoverService _handoverService;

    public PaymentService(
        UCarDbContext context, 
        ILogger<PaymentService> logger,
        IInvoiceService invoiceService,
        IHandoverService handoverService)
    {
        _context = context;
        _logger = logger;
        _invoiceService = invoiceService;
        _handoverService = handoverService;
    }

    #region Pickup Payment - Thanh toán khi nhận xe (Luồng mới)
    
    public async Task<PickupPaymentViewModel?> GetPickupPaymentInfoAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .Include(c => c.HandoverRecord).ThenInclude(h => h!.Accessories)
            .Include(c => c.CollateralItems)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return null;

        // Chỉ cho phép thanh toán khi hợp đồng ở trạng thái PendingSigning
        if (contract.Status != RentalContractStatus.PendingSigning)
        {
            _logger.LogWarning("Contract {ContractId} is not in PendingSigning status", contractId);
            return null;
        }

        // === TẠO HÓA ĐƠN GIAO XE TRƯỚC KHI THANH TOÁN ===
        // Nếu đã có hóa đơn thì trả về ID hiện tại để thanh toán lại
        var deliveryInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Delivery);
        
        if (deliveryInvoice == null)
        {
            try
            {
                // Tạo hóa đơn giao xe mới - IssuedBy cần là UserId (từ Customer.UserId)
                var invoiceId = await _invoiceService.CreateDeliveryInvoiceAsync(contractId, contract.Customer.UserId);
                deliveryInvoice = await _context.Invoices.FindAsync(invoiceId);
                _logger.LogInformation("Created Delivery Invoice {InvoiceId} for contract {ContractId} at GetPickupPaymentInfo",
                    invoiceId, contractId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Delivery Invoice for contract {ContractId}", contractId);
                // Nếu không tạo được invoice, vẫn trả về thông tin để hiển thị
            }
        }
        else
        {
            _logger.LogInformation("Found existing Delivery Invoice {InvoiceId} for contract {ContractId}",
                deliveryInvoice.InvoiceId, contractId);
        }

        // Hiện tại phụ kiện không tính thêm phí thuê
        var accessoryAmount = 0m;

        var rentalDays = (int)Math.Ceiling((contract.PlannedEnd - contract.PlannedStart).TotalDays);
        if (rentalDays < 1) rentalDays = 1;

        // Lấy số tiền từ Invoice nếu có, không thì tính từ contract
        var totalPickupAmount = deliveryInvoice?.TotalAmount ?? (contract.RentalAmount + contract.SnapshotDepositAmount);
        var amountPaid = deliveryInvoice?.AmountPaid ?? 0;
        var amountDue = deliveryInvoice?.AmountDue ?? totalPickupAmount;

        return new PickupPaymentViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            PickupDate = contract.PlannedStart,
            ReturnDate = contract.PlannedEnd,
            RentalDays = rentalDays,
            BaseRentalAmount = contract.RentalAmount - accessoryAmount,
            AccessoryAmount = accessoryAmount,
            TotalRentalAmount = contract.RentalAmount,
            DepositAmount = contract.SnapshotDepositAmount,
            TotalPickupAmount = totalPickupAmount, // Tổng số tiền cần thanh toán
            PaymentAmount = amountDue, // Số tiền còn phải thanh toán (có thể đã thanh toán một phần)
            InvoiceId = deliveryInvoice?.InvoiceId, // ID hóa đơn để thanh toán
            HandoverId = contract.HandoverRecord?.HandoverId, // ID biên bản giao xe
            CollateralItems = contract.CollateralItems?.Select(c => new CollateralItemViewModel
            {
                ItemId = c.CollateralId,
                ItemType = c.ItemType.ToString(),
                Description = c.Description ?? "",
                EstimatedValue = c.EstimatedValue
            }).ToList() ?? new List<CollateralItemViewModel>()
        };
    }

    public async Task<ServiceResult<Guid>> ProcessPickupPaymentAsync(PickupPaymentViewModel model, Guid processedBy)
    {
        _logger.LogInformation("ProcessPickupPaymentAsync: Processing pickup payment for contract {ContractId}, amount {Amount}", 
            model.ContractId, model.PaymentAmount);

        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == model.ContractId);

        if (contract == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hợp đồng");

        if (contract.Status != RentalContractStatus.PendingSigning)
            return ServiceResult<Guid>.Fail("Hợp đồng không ở trạng thái chờ ký thanh toán");

        // Lấy hóa đơn giao xe (đã được tạo trước đó trong GetPickupPaymentInfoAsync)
        var deliveryInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.ContractId == model.ContractId && i.InvoiceType == InvoiceType.Delivery);

        if (deliveryInvoice == null)
        {
            // Nếu chưa có invoice thì tạo mới (fallback)
            try 
            {
                var invoiceId = await _invoiceService.CreateDeliveryInvoiceAsync(model.ContractId, processedBy);
                deliveryInvoice = await _context.Invoices.FindAsync(invoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Delivery Invoice for contract {ContractId}", model.ContractId);
                return ServiceResult<Guid>.Fail("Không thể tạo hóa đơn giao xe");
            }
        }

        if (deliveryInvoice == null)
            return ServiceResult<Guid>.Fail("Không tìm thấy hóa đơn giao xe");

        // Kiểm tra số tiền thanh toán
        var requiredAmount = deliveryInvoice.AmountDue;
        if (model.PaymentAmount < requiredAmount)
        {
            return ServiceResult<Guid>.Fail($"Số tiền thanh toán không đủ. Yêu cầu tối thiểu: {requiredAmount:N0}đ");
        }

        // Tạo transaction thanh toán
        var transaction = new PaymentTransaction
        {
            TxnId = Guid.NewGuid(),
            ContractId = model.ContractId,
            CustomerId = contract.CustomerId,
            TxnType = TransactionType.PickupPayment,
            Amount = model.PaymentAmount,
            PaymentMethod = model.PaymentMethod,
            BankRefCode = model.BankRefCode,
            PaidAt = DateTime.Now,
            Status = TransactionStatus.Success,
            RefType = ReferenceType.Invoice,
            RefId = deliveryInvoice.InvoiceId,
            Note = model.Note ?? "Thanh toán khi nhận xe"
        };

        _context.PaymentTransactions.Add(transaction);

        // Cập nhật invoice là đã thanh toán
        deliveryInvoice.AmountPaid += model.PaymentAmount;
        deliveryInvoice.AmountDue = Math.Max(0, deliveryInvoice.TotalAmount - deliveryInvoice.AmountPaid);
        deliveryInvoice.Status = deliveryInvoice.AmountDue == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        deliveryInvoice.UpdatedAt = DateTime.Now;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Delivery invoice {InvoiceId} marked as paid for contract {ContractId}", 
            deliveryInvoice.InvoiceId, model.ContractId);

        // Chỉ hoàn tất giao xe khi hóa đơn đã thanh toán đủ
        if (deliveryInvoice.Status == InvoiceStatus.Paid)
        {
            var handoverResult = await _handoverService.CompleteHandoverAsync(
                contract.ContractId, 
                processedBy, 
                transaction.TxnId);

            if (!handoverResult.Success)
            {
                _logger.LogError("Failed to complete handover for contract {ContractId}: {Error}", 
                    model.ContractId, handoverResult.Message);
                // Transaction đã tạo nên vẫn thành công, chỉ log warning
            }
        }
        else
        {
            _logger.LogWarning("Invoice {InvoiceId} not fully paid yet. AmountDue: {AmountDue}. Handover not completed.",
                deliveryInvoice.InvoiceId, deliveryInvoice.AmountDue);
        }

        _logger.LogInformation("Pickup payment processed successfully. TxnId: {TxnId}, Contract: {ContractId}", 
            transaction.TxnId, model.ContractId);

        return ServiceResult<Guid>.Ok(transaction.TxnId, "Thanh toán và giao xe thành công!");
    }
    
    #endregion

    #region Return Payment - Thanh toán khi trả xe
    
    public async Task<PaymentProcessViewModel?> GetPaymentInfoAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Vehicle).ThenInclude(v => v.Model)
            .Include(c => c.Charges)
            .Include(c => c.PaymentTransactions)
            .Include(c => c.ReturnRecord)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null || contract.ReturnRecord == null)
            return null;

        var totalPaid = contract.PaymentTransactions
            .Where(p => p.Status == TransactionStatus.Success && 
                        p.TxnType != TransactionType.RefundResponsibility && 
                        p.TxnType != TransactionType.RefundRental)
            .Sum(p => p.Amount);

        var totalCharges = contract.Charges.Sum(c => c.Amount);
        var grandTotal = contract.RentalAmount + totalCharges;
        var amountDue = grandTotal - contract.SnapshotDepositAmount;

        return new PaymentProcessViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            PlateNo = contract.Vehicle.PlateNo,
            VehicleName = $"{contract.Vehicle.Model.Make} {contract.Vehicle.Model.ModelName}",
            RentalAmount = contract.RentalAmount,
            ExtraCharges = totalCharges,
            GrandTotal = grandTotal,
            DepositAmount = contract.SnapshotDepositAmount,
            AmountDue = amountDue,
            AmountPaid = totalPaid,
            PaymentAmount = Math.Max(0, amountDue - totalPaid)
        };
    }

    public async Task<ServiceResult> ProcessPaymentAsync(PaymentProcessViewModel model, Guid processedBy)
    {
        _logger.LogInformation("ProcessPaymentAsync: Processing payment for contract {ContractId}, amount {Amount}", 
            model.ContractId, model.PaymentAmount);

        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == model.ContractId);

        if (contract == null)
            return ServiceResult.Fail("Không tìm thấy hợp đồng");

        if (model.PaymentAmount <= 0)
            return ServiceResult.Fail("Số tiền thanh toán phải lớn hơn 0");

        var transaction = new PaymentTransaction
        {
            TxnId = Guid.NewGuid(),
            ContractId = model.ContractId,
            CustomerId = contract.CustomerId,
            TxnType = TransactionType.RentalFee,
            Amount = model.PaymentAmount,
            PaymentMethod = model.PaymentMethod,
            BankRefCode = model.BankRefCode,
            PaidAt = DateTime.Now,
            Status = TransactionStatus.Success,
            RefType = ReferenceType.Contract,
            RefId = model.ContractId,
            Note = model.Note
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // === LUỒNG MỚI: Hoàn tất trả xe và tạo Return Invoice ===
        var returnResult = await _handoverService.CompleteReturnAsync(
            model.ContractId,
            processedBy,
            transaction.TxnId);

        if (!returnResult.Success)
        {
            _logger.LogWarning("Failed to complete return for contract {ContractId}: {Error}. Payment still succeeded.",
                model.ContractId, returnResult.Message);
        }

        _logger.LogInformation("Payment processed successfully. TxnId: {TxnId}", transaction.TxnId);

        return ServiceResult.Ok("Thanh toán thành công!");
    }

    public async Task<RefundProcessViewModel?> GetRefundInfoAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer).ThenInclude(cu => cu.UserAccount)
            .Include(c => c.Charges)
            .Include(c => c.ReturnRecord)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null || contract.ReturnRecord == null)
            return null;

        var totalCharges = contract.Charges.Sum(c => c.Amount);
        var grandTotal = contract.RentalAmount + totalCharges;
        var amountDue = grandTotal - contract.SnapshotDepositAmount;

        // Only show refund if deposit > total cost
        if (amountDue >= 0)
            return null;

        return new RefundProcessViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            CustomerPhone = contract.Customer.UserAccount.Phone ?? "",
            GrandTotal = grandTotal,
            DepositAmount = contract.SnapshotDepositAmount,
            AmountDue = amountDue
        };
    }

    public async Task<ServiceResult> ProcessRefundAsync(RefundProcessViewModel model, Guid processedBy)
    {
        _logger.LogInformation("ProcessRefundAsync: Processing refund for contract {ContractId}", model.ContractId);

        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.ContractId == model.ContractId);

        if (contract == null)
            return ServiceResult.Fail("Không tìm thấy hợp đồng");

        var transaction = new PaymentTransaction
        {
            TxnId = Guid.NewGuid(),
            ContractId = model.ContractId,
            CustomerId = contract.CustomerId,
            TxnType = TransactionType.RefundRental,
            Amount = model.RefundAmount,
            PaymentMethod = model.RefundMethod,
            BankRefCode = model.BankRefCode,
            PaidAt = DateTime.Now,
            Status = TransactionStatus.Success,
            RefType = ReferenceType.Contract,
            RefId = model.ContractId,
            Note = model.Note ?? "Hoàn tiền cọc dư"
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // === LUỒNG MỚI: Hoàn tất trả xe và tạo Return Invoice ===
        var returnResult = await _handoverService.CompleteReturnAsync(
            model.ContractId,
            processedBy,
            transaction.TxnId);

        if (!returnResult.Success)
        {
            _logger.LogWarning("Failed to complete return for contract {ContractId}: {Error}. Refund still succeeded.",
                model.ContractId, returnResult.Message);
        }

        _logger.LogInformation("Refund processed successfully. TxnId: {TxnId}", transaction.TxnId);

        return ServiceResult.Ok("Hoàn tiền thành công!");
    }
    
    #endregion

    public async Task<PaymentHistoryViewModel> GetPaymentHistoryAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
            .Include(c => c.PaymentTransactions)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            return new PaymentHistoryViewModel();

        var transactions = contract.PaymentTransactions
            .OrderByDescending(t => t.PaidAt)
            .Select(t => new PaymentTransactionViewModel
            {
                TxnId = t.TxnId,
                TxnType = t.TxnType,
                Amount = t.Amount,
                PaymentMethod = t.PaymentMethod,
                BankRefCode = t.BankRefCode,
                PaidAt = t.PaidAt,
                Status = t.Status,
                Note = t.Note
            })
            .ToList();

        // Các loại transaction là thanh toán (không phải hoàn tiền)
        var paymentTypes = new[] 
        { 
            TransactionType.ResponsibilityDeposit,
            TransactionType.RentalDeposit,
            TransactionType.RentalFee,
            TransactionType.Penalty,
            TransactionType.PickupPayment,
            TransactionType.ReturnPayment
        };
        
        // Các loại transaction là hoàn tiền
        var refundTypes = new[] 
        { 
            TransactionType.RefundResponsibility, 
            TransactionType.RefundRental,
            TransactionType.ReturnRefund
        };

        var totalPaid = transactions
            .Where(t => t.Status == TransactionStatus.Success && paymentTypes.Contains(t.TxnType))
            .Sum(t => t.Amount);

        var totalRefunded = transactions
            .Where(t => t.Status == TransactionStatus.Success && refundTypes.Contains(t.TxnType))
            .Sum(t => t.Amount);

        return new PaymentHistoryViewModel
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode,
            CustomerName = contract.Customer.FullName,
            TotalAmount = contract.TotalAmountFinal,
            TotalPaid = totalPaid,
            TotalRefunded = totalRefunded,
            Balance = contract.TotalAmountFinal - contract.SnapshotDepositAmount - totalPaid + totalRefunded,
            Transactions = transactions
        };
    }
}
