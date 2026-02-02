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

    public PaymentService(UCarDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

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

        _logger.LogInformation("Refund processed successfully. TxnId: {TxnId}", transaction.TxnId);

        return ServiceResult.Ok("Hoàn tiền thành công!");
    }

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

        var totalPaid = transactions
            .Where(t => t.Status == TransactionStatus.Success && 
                        t.TxnType != TransactionType.RefundResponsibility && 
                        t.TxnType != TransactionType.RefundRental)
            .Sum(t => t.Amount);

        var totalRefunded = transactions
            .Where(t => t.Status == TransactionStatus.Success && 
                        (t.TxnType == TransactionType.RefundResponsibility || 
                         t.TxnType == TransactionType.RefundRental))
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
