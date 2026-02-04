using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.Enums;

namespace UCar.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly UCarDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(UCarDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== INVOICE CREATION =====

        public async Task<Guid> CreateDepositInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                .Include(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
                        .ThenInclude(vm => vm.VehicleType)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Kiểm tra đã có Deposit Invoice chưa
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Deposit);

            if (existingInvoice != null)
                throw new InvalidOperationException($"Deposit Invoice already exists for contract {contract.ContractCode}");

            // Sử dụng giá trị deposit đã lưu trong contract thay vì tính lại
            // Contract đã tính chính xác khi tạo dựa trên IPriceCalculationService
            var responsibilityDeposit = contract.ResponsibilityDeposit;
            var rentalDeposit = contract.RentalDeposit;
            var totalDeposit = responsibilityDeposit + rentalDeposit;

            // Tạo Invoice
            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync("DEP"),
                InvoiceType = InvoiceType.Deposit,
                ContractId = contractId,
                CustomerId = contract.CustomerId,
                IssuedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(3), // Phải trả trong 3 ngày

                BaseRentalAmount = 0,
                SurchargesTotal = 0,
                PenaltiesTotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                DepositPaid = 0,

                TotalAmount = totalDeposit,
                AmountPaid = 0,
                AmountDue = totalDeposit,

                Status = InvoiceStatus.Issued,
                IssuedBy = issuedBy,
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            // Tạo Line Items
            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.ResponsibilityDeposit,
                Description = $"Cọc trách nhiệm - {contract.Vehicle.Model.VehicleType.TypeName}",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = responsibilityDeposit,
                Amount = responsibilityDeposit,
                CreatedAt = DateTime.Now
            });

            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.RentalDeposit,
                Description = $"Cọc thuê xe - Hợp đồng {contract.ContractCode}",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = rentalDeposit,
                Amount = rentalDeposit,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created Deposit Invoice {invoice.InvoiceNumber} for contract {contract.ContractCode} - Total: {totalDeposit:N0} VNĐ");

            return invoice.InvoiceId;
        }

        public async Task<Guid> CreateRentalInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Kiểm tra đã có Rental Invoice chưa
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Rental);

            if (existingInvoice != null)
                throw new InvalidOperationException($"Rental Invoice already exists for contract {contract.ContractCode}");

            // Lấy thông tin từ snapshot pricing
            var baseRentalAmount = contract.RentalAmount;
            var rentalDepositPaid = contract.RentalDeposit;
            var amountDue = baseRentalAmount - rentalDepositPaid;

            // Tạo Invoice
            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync("RNT"),
                InvoiceType = InvoiceType.Rental,
                ContractId = contractId,
                CustomerId = contract.CustomerId,
                IssuedDate = DateTime.Now,
                DueDate = DateTime.Now, // Phải trả ngay khi nhận xe

                BaseRentalAmount = baseRentalAmount,
                SurchargesTotal = 0,
                PenaltiesTotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                DepositPaid = rentalDepositPaid,

                TotalAmount = baseRentalAmount,
                AmountPaid = rentalDepositPaid, // Đã trả cọc
                AmountDue = amountDue,

                Status = amountDue <= 0 ? InvoiceStatus.Paid : InvoiceStatus.Issued,
                IssuedBy = issuedBy,
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            // Tạo Line Items từ snapshot pricing
            if (contract.IsMonthlyRate)
            {
                // Thuê theo tháng
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    LineItemId = Guid.NewGuid(),
                    InvoiceId = invoice.InvoiceId,
                    ItemType = LineItemType.MonthlyRental,
                    Description = $"Thuê xe theo tháng",
                    Quantity = 1,
                    Unit = "tháng",
                    UnitPrice = contract.MonthlyAmount,
                    Amount = contract.MonthlyAmount,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                // Ngày thường
                if (contract.NormalDays > 0)
                {
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        LineItemId = Guid.NewGuid(),
                        InvoiceId = invoice.InvoiceId,
                        ItemType = LineItemType.BaseRental,
                        Description = $"Thuê xe ngày thường",
                        Quantity = contract.NormalDays,
                        Unit = "ngày",
                        UnitPrice = contract.SnapshotBaseDailyPrice,
                        Amount = contract.NormalDaysAmount,
                        CreatedAt = DateTime.Now
                    });
                }

                // Ngày lễ
                if (contract.PeakDays > 0)
                {
                    var peakDailyPrice = contract.SnapshotBaseDailyPrice * contract.SnapshotPeakMultiplier;
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        LineItemId = Guid.NewGuid(),
                        InvoiceId = invoice.InvoiceId,
                        ItemType = LineItemType.PeakRental,
                        Description = $"Thuê xe ngày lễ (x{contract.SnapshotPeakMultiplier})",
                        Quantity = contract.PeakDays,
                        Unit = "ngày",
                        UnitPrice = peakDailyPrice,
                        Amount = contract.PeakDaysAmount,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Trừ cọc thuê xe đã trả
            if (rentalDepositPaid > 0)
            {
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    LineItemId = Guid.NewGuid(),
                    InvoiceId = invoice.InvoiceId,
                    ItemType = LineItemType.RentalDeposit,
                    Description = $"Trừ cọc thuê xe đã thanh toán",
                    Quantity = 1,
                    Unit = "lần",
                    UnitPrice = -rentalDepositPaid,
                    Amount = -rentalDepositPaid,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created Rental Invoice {invoice.InvoiceNumber} for contract {contract.ContractCode} - Amount Due: {amountDue:N0} VNĐ");

            return invoice.InvoiceId;
        }

        public async Task<Guid?> CreateSurchargePenaltyInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                .Include(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
                        .ThenInclude(vm => vm.VehicleType)
                .Include(c => c.Charges)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Lấy các charges chưa thanh toán
            var unpaidCharges = contract.Charges.Where(ch => !ch.IsPaid).ToList();

            if (!unpaidCharges.Any())
            {
                _logger.LogInformation($"No unpaid charges found for contract {contract.ContractCode}. Skip creating Surcharge/Penalty Invoice.");
                return null; // Không có charges thì không tạo invoice
            }

            // Kiểm tra đã có Surcharge/Penalty Invoice chưa
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && 
                    (i.InvoiceType == InvoiceType.Surcharge || i.InvoiceType == InvoiceType.Penalty));

            if (existingInvoice != null)
                throw new InvalidOperationException($"Surcharge/Penalty Invoice already exists for contract {contract.ContractCode}");

            var vehicleTypeId = contract.Vehicle.Model.VehicleTypeId;

            decimal surchargesTotal = 0;
            decimal penaltiesTotal = 0;
            var lineItems = new List<InvoiceLineItem>();

            foreach (var charge in unpaidCharges)
            {
                var lineItemType = DetermineLineItemTypeFromCharge(charge);
                var isSurcharge = lineItemType >= LineItemType.OvertimeSurcharge && lineItemType <= LineItemType.FuelSurcharge;

                if (isSurcharge)
                    surchargesTotal += charge.Amount;
                else
                    penaltiesTotal += charge.Amount;

                lineItems.Add(new InvoiceLineItem
                {
                    LineItemId = Guid.NewGuid(),
                    ItemType = lineItemType,
                    Description = charge.Description ?? charge.ChargeType.ToString(),
                    Quantity = 1,
                    Unit = "lần",
                    UnitPrice = charge.Amount,
                    Amount = charge.Amount,
                    ReferenceId = charge.ChargeId,
                    CreatedAt = DateTime.Now
                });
            }

            var totalAmount = surchargesTotal + penaltiesTotal;
            var invoiceType = surchargesTotal >= penaltiesTotal ? InvoiceType.Surcharge : InvoiceType.Penalty;
            var prefix = invoiceType == InvoiceType.Surcharge ? "SUR" : "PEN";

            // Tạo Invoice
            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync(prefix),
                InvoiceType = invoiceType,
                ContractId = contractId,
                CustomerId = contract.CustomerId,
                IssuedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7), // Phải trả trong 7 ngày

                BaseRentalAmount = 0,
                SurchargesTotal = surchargesTotal,
                PenaltiesTotal = penaltiesTotal,
                DiscountAmount = 0,
                TaxAmount = 0,
                DepositPaid = 0,

                TotalAmount = totalAmount,
                AmountPaid = 0,
                AmountDue = totalAmount,

                Status = InvoiceStatus.Issued,
                IssuedBy = issuedBy,
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            // Thêm line items
            foreach (var lineItem in lineItems)
            {
                lineItem.InvoiceId = invoice.InvoiceId;
                invoice.LineItems.Add(lineItem);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {invoiceType} Invoice {invoice.InvoiceNumber} for contract {contract.ContractCode} - Total: {totalAmount:N0} VNĐ");

            return invoice.InvoiceId;
        }

        public async Task<Guid> CreateRefundInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                .Include(c => c.Charges)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Kiểm tra đã có Refund Invoice chưa
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Refund);

            if (existingInvoice != null)
                throw new InvalidOperationException($"Refund Invoice already exists for contract {contract.ContractCode}");

            // Tính tổng cọc đã trả
            var totalDepositPaid = contract.ResponsibilityDeposit + contract.RentalDeposit;

            // Tính tổng charges chưa thanh toán
            var unpaidCharges = contract.Charges.Where(ch => !ch.IsPaid).Sum(ch => ch.Amount);

            // Số tiền hoàn lại = Tổng cọc - Các khoản chưa thanh toán
            var refundAmount = totalDepositPaid - unpaidCharges;

            // Tạo Invoice (negative amount = refund)
            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync("REF"),
                InvoiceType = InvoiceType.Refund,
                ContractId = contractId,
                CustomerId = contract.CustomerId,
                IssuedDate = DateTime.Now,
                DueDate = contract.DepositRefundDueDate ?? DateTime.Now.AddDays(30),

                BaseRentalAmount = 0,
                SurchargesTotal = 0,
                PenaltiesTotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                DepositPaid = totalDepositPaid,

                TotalAmount = -refundAmount, // Negative = refund to customer
                AmountPaid = 0,
                AmountDue = -refundAmount,

                Status = InvoiceStatus.Issued,
                IssuedBy = issuedBy,
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            // Line Items
            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.RefundResponsibility,
                Description = $"Hoàn cọc trách nhiệm",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = -contract.ResponsibilityDeposit,
                Amount = -contract.ResponsibilityDeposit,
                CreatedAt = DateTime.Now
            });

            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.RefundRental,
                Description = $"Hoàn cọc thuê xe",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = -contract.RentalDeposit,
                Amount = -contract.RentalDeposit,
                CreatedAt = DateTime.Now
            });

            if (unpaidCharges > 0)
            {
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    LineItemId = Guid.NewGuid(),
                    InvoiceId = invoice.InvoiceId,
                    ItemType = LineItemType.Other,
                    Description = $"Trừ các khoản phí chưa thanh toán",
                    Quantity = 1,
                    Unit = "lần",
                    UnitPrice = unpaidCharges,
                    Amount = unpaidCharges,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created Refund Invoice {invoice.InvoiceNumber} for contract {contract.ContractCode} - Refund Amount: {refundAmount:N0} VNĐ");

            return invoice.InvoiceId;
        }

        // ===== LUỒNG MỚI: DELIVERY & RETURN INVOICES =====

        /// <summary>
        /// LUỒNG MỚI: Tạo hóa đơn giao xe (Delivery Invoice)
        /// Bao gồm: Cọc trách nhiệm + Tiền thuê + Cọc thuê
        /// NOTE: Phụ kiện chỉ là vật dụng đi kèm, không tính phí
        /// </summary>
        public async Task<Guid> CreateDeliveryInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                .Include(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
                        .ThenInclude(vm => vm.VehicleType)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Retry loop để handle race condition
            const int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // Kiểm tra đã có Delivery Invoice chưa - mỗi lần retry đều check lại
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Delivery);

                if (existingInvoice != null)
                {
                    _logger.LogInformation("Delivery Invoice {InvoiceNumber} already exists for contract {ContractCode}",
                        existingInvoice.InvoiceNumber, contract.ContractCode);
                    return existingInvoice.InvoiceId; // Trả về ID hiện tại để thanh toán lại
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Double check bên trong transaction
                    existingInvoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Delivery);

                    if (existingInvoice != null)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogInformation("Delivery Invoice {InvoiceNumber} already exists (found in transaction)",
                            existingInvoice.InvoiceNumber);
                        return existingInvoice.InvoiceId;
                    }

                    // Lấy các khoản từ contract
                    var responsibilityDeposit = contract.ResponsibilityDeposit;
                    var rentalDeposit = contract.RentalDeposit;
                    var rentalAmount = contract.RentalAmount;

                    // TỔNG: Cọc trách nhiệm + Tiền thuê + Cọc thuê (không tính phụ kiện)
                    var totalAmount = responsibilityDeposit + rentalAmount + rentalDeposit;

                    // Generate số invoice mới mỗi lần retry
                    var invoiceNumber = await GenerateInvoiceNumberAsync("DLV");

                    // Tạo Invoice
                    var invoice = new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        InvoiceNumber = invoiceNumber,
                        InvoiceType = InvoiceType.Delivery,
                        ContractId = contractId,
                        CustomerId = contract.CustomerId,
                        IssuedDate = DateTime.Now,
                        DueDate = DateTime.Now, // Phải trả ngay tại quầy

                        BaseRentalAmount = rentalAmount,
                        SurchargesTotal = 0,
                        PenaltiesTotal = 0,
                        DiscountAmount = 0,
                        TaxAmount = 0,
                        DepositPaid = 0,

                        TotalAmount = totalAmount,
                        AmountPaid = 0,
                        AmountDue = totalAmount,

                        Status = InvoiceStatus.Issued,
                        IssuedBy = issuedBy,
                        Notes = "Hóa đơn giao xe - Thanh toán tại quầy",
                        CreatedAt = DateTime.Now
                    };

                    _context.Invoices.Add(invoice);
                    
                    // Save Invoice trước để có InvoiceId cho FK
                    await _context.SaveChangesAsync();

                    // Line Items
                    var lineItems = new List<InvoiceLineItem>();
                    
                    // 1. Cọc trách nhiệm
                    lineItems.Add(new InvoiceLineItem
                    {
                        LineItemId = Guid.NewGuid(),
                        InvoiceId = invoice.InvoiceId,
                        ItemType = LineItemType.ResponsibilityDeposit,
                        Description = $"Cọc trách nhiệm - {contract.Vehicle.Model.VehicleType.TypeName}",
                        Quantity = 1,
                        Unit = "lần",
                        UnitPrice = responsibilityDeposit,
                    Amount = responsibilityDeposit,
                    CreatedAt = DateTime.Now
                });

                // 2. Cọc thuê xe
                lineItems.Add(new InvoiceLineItem
                {
                    LineItemId = Guid.NewGuid(),
                    InvoiceId = invoice.InvoiceId,
                    ItemType = LineItemType.RentalDeposit,
                    Description = $"Cọc thuê xe - 50% giá trị hợp đồng",
                    Quantity = 1,
                    Unit = "lần",
                    UnitPrice = rentalDeposit,
                    Amount = rentalDeposit,
                    CreatedAt = DateTime.Now
                });

                // 3. Tiền thuê xe
                if (contract.IsMonthlyRate)
                {
                    lineItems.Add(new InvoiceLineItem
                    {
                        LineItemId = Guid.NewGuid(),
                        InvoiceId = invoice.InvoiceId,
                        ItemType = LineItemType.MonthlyRental,
                        Description = $"Thuê xe theo tháng",
                        Quantity = 1,
                        Unit = "tháng",
                        UnitPrice = contract.MonthlyAmount,
                        Amount = contract.MonthlyAmount,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    // Ngày thường
                    if (contract.NormalDays > 0)
                    {
                        lineItems.Add(new InvoiceLineItem
                        {
                            LineItemId = Guid.NewGuid(),
                            InvoiceId = invoice.InvoiceId,
                            ItemType = LineItemType.BaseRental,
                            Description = $"Thuê xe ngày thường",
                            Quantity = contract.NormalDays,
                            Unit = "ngày",
                            UnitPrice = contract.SnapshotBaseDailyPrice,
                            Amount = contract.NormalDaysAmount,
                            CreatedAt = DateTime.Now
                        });
                    }

                    // Ngày lễ
                    if (contract.PeakDays > 0)
                    {
                        var peakDailyPrice = contract.SnapshotBaseDailyPrice * contract.SnapshotPeakMultiplier;
                        lineItems.Add(new InvoiceLineItem
                        {
                            LineItemId = Guid.NewGuid(),
                            InvoiceId = invoice.InvoiceId,
                            ItemType = LineItemType.PeakRental,
                            Description = $"Thuê xe ngày lễ (x{contract.SnapshotPeakMultiplier})",
                            Quantity = contract.PeakDays,
                            Unit = "ngày",
                            UnitPrice = peakDailyPrice,
                            Amount = contract.PeakDaysAmount,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                _context.InvoiceLineItems.AddRange(lineItems);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                _logger.LogInformation("Created Delivery Invoice {InvoiceNumber} for contract {ContractCode} - Total: {TotalAmount:N0} VNĐ",
                    invoice.InvoiceNumber, contract.ContractCode, totalAmount);

                return invoice.InvoiceId;
                }
                catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 2601)
                {
                    // Duplicate key error - retry với số invoice mới
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Duplicate invoice number detected on attempt {Attempt}, retrying...", attempt);
                    
                    // Detach tất cả entities đã track để tránh lỗi khi retry
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Failed to create Delivery Invoice after {MaxRetries} attempts", maxRetries);
                        throw new InvalidOperationException($"Không thể tạo hóa đơn sau {maxRetries} lần thử. Vui lòng thử lại.", ex);
                    }
                    
                    // Wait a bit before retry to reduce collision chance
                    await Task.Delay(100 * attempt);
                    continue;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to create Delivery Invoice for contract {ContractId}", contractId);
                    throw;
                }
            }

            // Should never reach here
            throw new InvalidOperationException("Failed to create Delivery Invoice - unexpected error");
        }

        /// <summary>
        /// LUỒNG MỚI: Tạo hóa đơn trả xe (Return Invoice)
        /// CHỈ tính hoàn cọc, KHÔNG tính phí phát sinh (phí đã có Surcharge Invoice riêng)
        /// - Hoàn cọc trách nhiệm
        /// - Hoàn cọc thuê xe
        /// </summary>
        public async Task<Guid> CreateReturnInvoiceAsync(Guid contractId, Guid issuedBy)
        {
            var contract = await _context.RentalContracts
                .Include(c => c.Customer)
                    .ThenInclude(cu => cu.UserAccount)
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new InvalidOperationException($"Contract {contractId} not found");

            // Kiểm tra đã có Return Invoice chưa
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Return);

            if (existingInvoice != null)
            {
                _logger.LogInformation("Return Invoice {InvoiceNumber} already exists for contract {ContractCode}",
                    existingInvoice.InvoiceNumber, contract.ContractCode);
                return existingInvoice.InvoiceId;
            }

            // Tính tổng cọc cần hoàn (Cọc trách nhiệm + Cọc thuê)
            // KHÔNG tính tiền thuê vì đã thanh toán khi giao xe
            var responsibilityDeposit = contract.ResponsibilityDeposit;
            var rentalDeposit = contract.RentalDeposit;
            var totalRefund = responsibilityDeposit + rentalDeposit;

            // TotalAmount âm = Hoàn tiền cho khách
            var invoiceTotalAmount = -totalRefund;

            // Tạo Invoice
            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync("RTN"),
                InvoiceType = InvoiceType.Return,
                ContractId = contractId,
                CustomerId = contract.CustomerId,
                IssuedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7), // Hoàn tiền trong 7 ngày

                BaseRentalAmount = 0,
                SurchargesTotal = 0, // Phí đã có invoice riêng
                PenaltiesTotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                DepositPaid = totalRefund,

                TotalAmount = invoiceTotalAmount, // Số âm = hoàn tiền
                AmountPaid = 0,
                AmountDue = invoiceTotalAmount,

                Status = InvoiceStatus.Issued,
                IssuedBy = issuedBy,
                Notes = $"Hoàn tiền cọc cho khách hàng: {totalRefund:N0} VNĐ (Cọc TN: {responsibilityDeposit:N0} + Cọc thuê: {rentalDeposit:N0})",
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            // Line Items
            // 1. Hoàn cọc trách nhiệm
            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.RefundResponsibility,
                Description = "Hoàn cọc trách nhiệm",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = -responsibilityDeposit, // Số âm = hoàn tiền
                Amount = -responsibilityDeposit,
                CreatedAt = DateTime.Now
            });

            // 2. Hoàn cọc thuê xe
            invoice.LineItems.Add(new InvoiceLineItem
            {
                LineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                ItemType = LineItemType.RefundRental,
                Description = "Hoàn cọc thuê xe",
                Quantity = 1,
                Unit = "lần",
                UnitPrice = -rentalDeposit, // Số âm = hoàn tiền
                Amount = -rentalDeposit,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created Return Invoice {InvoiceNumber} for contract {ContractCode} - REFUND: {TotalRefund:N0} VNĐ",
                invoice.InvoiceNumber, contract.ContractCode, totalRefund);

            return invoice.InvoiceId;
        }

        /// <summary>
        /// Kiểm tra ChargeType có phải là Surcharge (phụ phí) hay không
        /// </summary>
        private bool IsSurcharge(ChargeType chargeType)
        {
            return chargeType switch
            {
                ChargeType.OvertimeFee => true,
                ChargeType.CleaningFee => true,
                ChargeType.FuelShortage => true,
                ChargeType.LateFee => true,
                _ => false
            };
        }

        // ===== HELPER METHODS =====

        private LineItemType DetermineLineItemTypeFromCharge(ContractCharge charge)
        {
            return charge.ChargeType switch
            {
                ChargeType.OvertimeFee => LineItemType.OvertimeSurcharge,
                ChargeType.LateFee => LineItemType.ExtraKmSurcharge, // Map LateFee to Extra KM for now
                ChargeType.CleaningFee => LineItemType.CleaningSurcharge,
                ChargeType.FuelShortage => LineItemType.FuelSurcharge,
                ChargeType.DamageFee => LineItemType.DamagePenalty,
                ChargeType.AccessoryLoss => LineItemType.ContractViolation,
                ChargeType.TollFee => LineItemType.Other,
                _ => LineItemType.Other
            };
        }

        public async Task<string> GenerateInvoiceNumberAsync(string prefix)
        {
            var yearMonth = DateTime.Now.ToString("yyMM");
            
            // Sử dụng vòng lặp để xử lý trường hợp concurrent request
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var lastInvoice = await _context.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith($"{prefix}-{yearMonth}-"))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastInvoice != null)
                {
                    var lastNumberStr = lastInvoice.InvoiceNumber.Split('-').Last();
                    if (int.TryParse(lastNumberStr, out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                var invoiceNumber = $"{prefix}-{yearMonth}-{nextNumber:D4}";
                
                // Kiểm tra xem số này đã tồn tại chưa
                var exists = await _context.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber);
                if (!exists)
                {
                    return invoiceNumber;
                }
                
                // Nếu trùng, thử lại với số tiếp theo
                _logger.LogWarning("Invoice number {InvoiceNumber} already exists, retrying...", invoiceNumber);
            }

            // Nếu vẫn không tìm được số hợp lệ, dùng GUID suffix
            var fallbackNumber = $"{prefix}-{yearMonth}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            _logger.LogWarning("Using fallback invoice number: {InvoiceNumber}", fallbackNumber);
            return fallbackNumber;
        }

        // ===== INVOICE QUERIES =====

        public async Task<(List<Invoice> Items, int TotalCount, int TotalPages)> GetInvoicesAsync(
            InvoiceStatus? status = null,
            InvoiceType? type = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 15)
        {
            var query = _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.Customer)
                .Include(i => i.IssuedByUser)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            if (type.HasValue)
                query = query.Where(i => i.InvoiceType == type.Value);

            if (fromDate.HasValue)
                query = query.Where(i => i.IssuedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.IssuedDate <= toDate.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(searchTerm) ||
                    i.Contract.ContractCode.Contains(searchTerm) ||
                    i.Customer.FullName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, totalPages);
        }

        public async Task<Invoice?> GetInvoiceDetailsAsync(Guid invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Vehicle)
                        .ThenInclude(v => v.Model)
                .Include(i => i.Customer)
                    .ThenInclude(c => c.UserAccount)
                .Include(i => i.IssuedByUser)
                .Include(i => i.LineItems)
                .Include(i => i.Adjustments)
                    .ThenInclude(a => a.AdjustedByStaff)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<List<Invoice>> GetInvoicesByContractAsync(Guid contractId)
        {
            return await _context.Invoices
                .Include(i => i.LineItems)
                .Where(i => i.ContractId == contractId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByTypeAndContractAsync(InvoiceType type, Guid contractId)
        {
            return await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.InvoiceType == type && i.ContractId == contractId);
        }

        // ===== PAYMENT OPERATIONS =====

        public async Task<InvoicePaymentInfoDto?> GetInvoicePaymentInfoAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.Customer)
                    .ThenInclude(c => c.UserAccount)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                return null;

            return new InvoicePaymentInfoDto
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceType = invoice.InvoiceType,
                InvoiceTypeDisplay = invoice.InvoiceType.ToString(),
                ContractId = invoice.ContractId,
                ContractCode = invoice.Contract.ContractCode,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.Customer.FullName,
                CustomerPhone = invoice.Customer.UserAccount.Phone ?? "",
                IssuedDate = invoice.IssuedDate,
                DueDate = invoice.DueDate,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                AmountDue = invoice.AmountDue,
                Status = invoice.Status,
                StatusDisplay = invoice.Status.ToString(),
                Notes = invoice.Notes,
                BaseRentalAmount = invoice.BaseRentalAmount,
                SurchargesTotal = invoice.SurchargesTotal,
                PenaltiesTotal = invoice.PenaltiesTotal,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                DepositPaid = invoice.DepositPaid
            };
        }

        public async Task<bool> IsInvoicePaidAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                return false;

            return invoice.Status == InvoiceStatus.Paid || invoice.AmountDue <= 0;
        }

        public async Task<Invoice?> GetInvoiceForPaymentAsync(Guid invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<bool> RecordPaymentAsync(
            Guid invoiceId, 
            decimal amount, 
            PaymentMethod paymentMethod,
            string? bankRefCode,
            Guid paidBy, 
            string? notes = null)
        {
            var invoice = await GetInvoiceForPaymentAsync(invoiceId);
            if (invoice == null)
            {
                _logger.LogError($"Invoice {invoiceId} not found for payment");
                return false;
            }

            if (amount <= 0)
            {
                _logger.LogError($"Invalid payment amount {amount} for Invoice {invoice.InvoiceNumber}");
                return false;
            }

            // Tạo PaymentTransaction
            var transaction = new PaymentTransaction
            {
                TxnId = Guid.NewGuid(),
                ContractId = invoice.ContractId,
                InvoiceId = invoice.InvoiceId,
                CustomerId = invoice.CustomerId,
                TxnType = MapInvoiceTypeToTransactionType(invoice.InvoiceType),
                Amount = amount,
                PaymentMethod = paymentMethod,
                BankRefCode = bankRefCode,
                PaidAt = DateTime.Now,
                Status = TransactionStatus.Success,
                RefType = ReferenceType.Invoice,
                RefId = invoice.InvoiceId,
                Note = notes
            };

            _context.PaymentTransactions.Add(transaction);

            // Cập nhật Invoice
            invoice.AmountPaid += amount;
            invoice.AmountDue = invoice.TotalAmount - invoice.AmountPaid;

            // Cập nhật status
            if (invoice.AmountDue <= 0)
            {
                invoice.Status = invoice.InvoiceType == InvoiceType.Refund 
                    ? InvoiceStatus.Refunded 
                    : InvoiceStatus.Paid;
            }
            else if (invoice.AmountPaid > 0 && invoice.AmountPaid < invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            invoice.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(notes))
            {
                invoice.InternalNotes = $"{invoice.InternalNotes}\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Payment: {amount:N0} VNĐ via {paymentMethod} - {notes}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Recorded payment {amount:N0} VNĐ for Invoice {invoice.InvoiceNumber}. TxnId: {transaction.TxnId}, New status: {invoice.Status}");

            return true;
        }

        private TransactionType MapInvoiceTypeToTransactionType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Deposit => TransactionType.Deposit,
                InvoiceType.Rental => TransactionType.RentalFee,
                InvoiceType.Surcharge => TransactionType.Surcharge,
                InvoiceType.Penalty => TransactionType.PenaltyFee,
                InvoiceType.Refund => TransactionType.RefundRental,
                _ => TransactionType.RentalFee
            };
        }

        // ===== INVOICE ADJUSTMENTS =====

        public async Task<Guid> AddAdjustmentAsync(
            Guid invoiceId,
            AdjustmentType adjustmentType,
            decimal amount,
            string reason,
            Guid adjustedBy,
            string? notes = null)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found");

            var adjustment = new InvoiceAdjustment
            {
                AdjustmentId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                AdjustmentType = adjustmentType,
                Amount = amount,
                Reason = reason,
                AdjustedBy = adjustedBy,
                AdjustedAt = DateTime.Now,
                Notes = notes
            };

            _context.InvoiceAdjustments.Add(adjustment);

            // Cập nhật invoice amounts
            invoice.TotalAmount += amount;
            invoice.AmountDue = invoice.TotalAmount - invoice.AmountPaid;
            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Added adjustment {amount:N0} VNĐ to Invoice {invoice.InvoiceNumber}. Reason: {reason}");

            return adjustment.AdjustmentId;
        }

        public async Task<bool> CancelInvoiceAsync(Guid invoiceId, string reason, Guid cancelledBy)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                return false;

            if (invoice.AmountPaid > 0)
                throw new InvalidOperationException($"Cannot cancel invoice {invoice.InvoiceNumber} - payment already recorded");

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.Now;
            invoice.InternalNotes = $"{invoice.InternalNotes}\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Cancelled by staff {cancelledBy}. Reason: {reason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cancelled Invoice {invoice.InvoiceNumber}. Reason: {reason}");

            return true;
        }

        // ===== BACKGROUND SERVICES =====

        public async Task ProcessRefundInvoicesAsync()
        {
            var today = DateTime.Now.Date;

            // Tìm các hợp đồng đã trả xe và quá thời hạn hoàn cọc, chưa có Refund Invoice
            var eligibleContracts = await _context.RentalContracts
                .Include(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
                        .ThenInclude(vm => vm.VehicleType)
                .Where(c => c.Status == RentalContractStatus.Completed &&
                            c.DepositRefundDueDate.HasValue &&
                            c.DepositRefundDueDate.Value <= today &&
                            !_context.Invoices.Any(i => i.ContractId == c.ContractId && i.InvoiceType == InvoiceType.Refund))
                .ToListAsync();

            foreach (var contract in eligibleContracts)
            {
                try
                {
                    // Sử dụng system admin user ID (hoặc lấy từ config)
                    var systemUserId = await _context.StaffProfiles.Select(s => s.StaffId).FirstOrDefaultAsync();
                    if (systemUserId != Guid.Empty)
                    {
                        await CreateRefundInvoiceAsync(contract.ContractId, systemUserId);
                        _logger.LogInformation($"Auto-created Refund Invoice for contract {contract.ContractCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create Refund Invoice for contract {contract.ContractCode}");
                }
            }
        }

        public async Task CheckOverdueInvoicesAsync()
        {
            var today = DateTime.Now.Date;

            var overdueInvoices = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued &&
                            i.DueDate.HasValue &&
                            i.DueDate.Value < today &&
                            i.AmountDue > 0)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.Now;

                _logger.LogWarning($"Invoice {invoice.InvoiceNumber} is now OVERDUE. Amount due: {invoice.AmountDue:N0} VNĐ");

                // TODO: Send email notification to customer
            }

            if (overdueInvoices.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
