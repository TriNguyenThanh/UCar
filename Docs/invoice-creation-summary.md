# Invoice Creation - Summary & Validation

## Tổng quan
Hệ thống tự động tạo 4 loại invoice tại các điểm trong vòng đời hợp đồng thuê xe:

1. **Deposit Invoice** - Khi xác nhận hợp đồng (Contract Active)
2. **Rental Invoice** - Khi checkout (giao xe)
3. **Surcharge/Penalty Invoice** - Khi checkin (nhận xe lại) nếu có phí phát sinh
4. **Refund Invoice** - Sau 15-30 ngày (hoàn cọc dư)

---

## 1. Deposit Invoice (Hoá đơn cọc)

### Thời điểm tạo
- **Trigger:** `ContractService.ConfirmContractAsync()` line 727
- **Timing:** Ngay sau khi Contract.Status = Active

### Công thức tính
```csharp
// Lấy policy từ database
DepositPolicy depositPolicy = await _context.DepositPolicies.FirstOrDefaultAsync();

// Cọc trách nhiệm (cố định)
decimal responsibilityDeposit = depositPolicy.ResponsibilityDepositValue;

// Cọc thuê xe (% giá trị hợp đồng)
decimal contractValue = contract.RentalAmount;
decimal rentalDeposit = contractValue * (depositPolicy.RentalDepositValue / 100);

// Tổng cọc
decimal totalDeposit = responsibilityDeposit + rentalDeposit;
```

### Line Items
- **Cọc trách nhiệm:** 1 line item với giá = `responsibilityDeposit`
- **Cọc thuê xe:** 1 line item với giá = `rentalDeposit` (X% giá trị hợp đồng)

### Validation
- InvoiceNumber: DEP-YYYYMMDD-XXXX
- TotalAmount = responsibilityDeposit + rentalDeposit
- AmountPaid = 0 (chưa thanh toán)
- AmountDue = TotalAmount
- Status = Issued
- DueDate = IssuedDate + 3 ngày

---

## 2. Rental Invoice (Hoá đơn thuê xe)

### Thời điểm tạo
- **Trigger:** `HandoverService.ConfirmCheckOutAsync()` line 271
- **Timing:** Ngay sau khi tạo HandoverRecord (checkout)

### Công thức tính
```csharp
// Lấy từ snapshot pricing trong contract
decimal baseRentalAmount = contract.RentalAmount;
decimal rentalDepositPaid = contract.RentalDeposit;

// Số tiền còn phải trả
decimal amountDue = baseRentalAmount - rentalDepositPaid;
```

### Line Items
**Nếu thuê theo tháng:**
- 1 line item: Thuê xe theo tháng (MonthlyAmount)

**Nếu thuê theo ngày:**
- **Ngày thường:** Quantity = NormalDays, UnitPrice = SnapshotBaseDailyPrice
- **Ngày lễ:** Quantity = PeakDays, UnitPrice = SnapshotBaseDailyPrice × SnapshotPeakMultiplier
- **Trừ cọc:** 1 line item âm với giá = -rentalDepositPaid

### Validation
- InvoiceNumber: RNT-YYYYMMDD-XXXX
- TotalAmount = baseRentalAmount
- AmountPaid = rentalDepositPaid (đã trả cọc)
- AmountDue = TotalAmount - AmountPaid
- Status = Issued (hoặc Paid nếu AmountDue <= 0)
- DueDate = IssuedDate (phải trả ngay)

---

## 3. Surcharge/Penalty Invoice (Hoá đơn phụ phí/phạt)

### Thời điểm tạo
- **Trigger:** `HandoverService.ConfirmCheckInAsync()` line 494
- **Timing:** Ngay sau khi tạo ReturnRecord (checkin), **chỉ nếu có charges chưa thanh toán**

### Công thức tính
```csharp
// Lấy tất cả charges chưa thanh toán
var unpaidCharges = contract.Charges.Where(ch => !ch.IsPaid).ToList();

// Phân loại
decimal surchargesTotal = unpaidCharges
    .Where(c => c.ChargeType is Overtime or FuelShortage or Cleaning or Damage)
    .Sum(c => c.Amount);

decimal penaltiesTotal = unpaidCharges
    .Where(c => c.ChargeType is TrafficViolation or LicenseRevoked or Accident)
    .Sum(c => c.Amount);

// Xác định loại invoice
InvoiceType type = surchargesTotal >= penaltiesTotal 
    ? InvoiceType.Surcharge 
    : InvoiceType.Penalty;

decimal totalAmount = surchargesTotal + penaltiesTotal;
```

### Line Items
- Mỗi ContractCharge chưa thanh toán → 1 line item
- Quantity = 1, UnitPrice = charge.Amount
- LineItemType mapping:
  - Overtime → OvertimeSurcharge
  - FuelShortage → FuelSurcharge
  - Cleaning → CleaningSurcharge
  - Damage → DamageSurcharge
  - TrafficViolation → TrafficViolationPenalty
  - LicenseRevoked → LicenseRevokedPenalty
  - Accident → AccidentPenalty

### Validation
- InvoiceNumber: SUR-YYYYMMDD-XXXX hoặc PEN-YYYYMMDD-XXXX
- TotalAmount = surchargesTotal + penaltiesTotal
- AmountPaid = 0
- AmountDue = TotalAmount
- Status = Issued
- DueDate = IssuedDate + 7 ngày
- **Lưu ý:** Return `null` nếu không có unpaid charges

---

## 4. Refund Invoice (Hoá đơn hoàn cọc)

### Thời điểm tạo
- **Trigger:** Manual call `InvoiceService.CreateRefundInvoiceAsync()`
- **Timing:** Sau 15-30 ngày khi contract completed

### Công thức tính
```csharp
// Tổng cọc đã trả
decimal totalDepositPaid = contract.ResponsibilityDeposit + contract.RentalDeposit;

// Tổng charges chưa thanh toán (nếu có)
decimal unpaidCharges = contract.Charges
    .Where(ch => !ch.IsPaid)
    .Sum(ch => ch.Amount);

// Số tiền hoàn lại (số âm)
decimal refundAmount = totalDepositPaid - unpaidCharges;
```

### Line Items
- **Hoàn cọc trách nhiệm:** -ResponsibilityDeposit (âm)
- **Hoàn cọc thuê xe:** -RentalDeposit (âm)
- **Trừ phí chưa trả (nếu có):** +unpaidCharges (dương)

### Validation
- InvoiceNumber: REF-YYYYMMDD-XXXX
- TotalAmount = **-refundAmount** (số âm = công ty trả khách)
- AmountPaid = 0
- AmountDue = -refundAmount
- Status = Issued
- DueDate = contract.DepositRefundDueDate hoặc IssuedDate + 30 ngày
- **Lưu ý:** TotalAmount và AmountDue là số âm (refund direction)

---

## Auto-creation Integration Points

### ContractService.ConfirmContractAsync (line 727)
```csharp
try
{
    var depositInvoiceId = await _invoiceService.CreateDepositInvoiceAsync(contractId, confirmedBy);
    
    if (depositInvoiceId != Guid.Empty)
    {
        _logger.LogInformation("Deposit Invoice {InvoiceId} created", depositInvoiceId);
    }
}
catch (Exception ex)
{
    // Don't fail contract confirmation if invoice fails
    _logger.LogError(ex, "Error creating Deposit Invoice");
}
```

### HandoverService.ConfirmCheckOutAsync (line 271)
```csharp
try
{
    var rentalInvoiceId = await _invoiceService.CreateRentalInvoiceAsync(dto.ContractId, userId);
}
catch (Exception ex)
{
    // Don't fail handover if invoice creation fails
    Console.WriteLine($"Error creating Rental Invoice: {ex.Message}");
}
```

### HandoverService.ConfirmCheckInAsync (line 494)
```csharp
try
{
    var hasCharges = await _context.ContractCharges
        .AnyAsync(c => c.ContractId == dto.ContractId && !c.IsPaid);
    
    if (hasCharges)
    {
        var surchargeInvoiceId = await _invoiceService
            .CreateSurchargePenaltyInvoiceAsync(dto.ContractId, userId);
    }
}
catch (Exception ex)
{
    // Don't fail check-in if invoice creation fails
    Console.WriteLine($"Error creating Surcharge Invoice: {ex.Message}");
}
```

---

## Duplicate Prevention

Tất cả methods đều check existing invoice trước khi tạo:

```csharp
var existingInvoice = await _context.Invoices
    .FirstOrDefaultAsync(i => i.ContractId == contractId && i.InvoiceType == InvoiceType.Deposit);

if (existingInvoice != null)
    throw new InvalidOperationException("Invoice already exists");
```

---

## Testing Checklist

### 1. Deposit Invoice
- [ ] Contract Active → Deposit Invoice tạo tự động
- [ ] TotalAmount = ResponsibilityDeposit + RentalDeposit (X% contract value)
- [ ] DueDate = IssuedDate + 3 ngày
- [ ] Line items: 2 items (responsibility + rental deposit)

### 2. Rental Invoice
- [ ] Checkout → Rental Invoice tạo tự động
- [ ] TotalAmount = contract.RentalAmount
- [ ] AmountPaid = contract.RentalDeposit (cọc đã trả)
- [ ] AmountDue = TotalAmount - AmountPaid
- [ ] Line items breakdown theo ngày thường/lễ

### 3. Surcharge Invoice
- [ ] Checkin with charges → Surcharge Invoice tạo
- [ ] Checkin without charges → Không tạo invoice
- [ ] TotalAmount = Sum of unpaid charges
- [ ] Line items: 1 per charge

### 4. Refund Invoice
- [ ] Manual creation sau 15-30 ngày
- [ ] TotalAmount = negative (company pays customer)
- [ ] Line items: 2 refund lines + 1 deduction (nếu có)

---

## Current Status

✅ **Hoàn tất:**
- [x] 4 invoice creation methods implemented
- [x] Auto-creation integrated tại 3 lifecycle stages
- [x] Duplicate prevention
- [x] Line items breakdown chi tiết
- [x] Logging và error handling

✅ **Database schema:**
- [x] Invoice model với PaymentTransactions navigation
- [x] InvoiceId FK trong PaymentTransaction
- [x] Enums updated (ReferenceType.Invoice, TransactionType.Deposit/Surcharge/PenaltyFee)

⏳ **Pending (sẽ do người khác làm):**
- [ ] Payment recording (RecordPaymentAsync đã implement, chờ UI/workflow)
- [ ] Payment validation rules
- [ ] PaymentService refactoring

---

## Notes

1. **Error Handling:** Tất cả auto-creation đều wrapped trong try-catch để không làm fail main workflow
2. **Transaction Safety:** Mỗi invoice creation là 1 transaction riêng
3. **Idempotency:** Check existing invoice trước khi tạo (không tạo duplicate)
4. **Negative Amounts:** Refund Invoice sử dụng số âm (TotalAmount < 0) để represent refund direction
5. **Snapshot Data:** Rental Invoice dùng snapshot pricing từ contract (không query realtime prices)
