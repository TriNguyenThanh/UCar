# WORKFLOW THANH TOÁN & XỬ LÝ PHỤ PHÍ - UCAR SYSTEM

## 📋 TỔNG QUAN

Hệ thống UCar áp dụng workflow thanh toán **nhiều giai đoạn** để xử lý phụ phí phát sinh sau khi trả xe.

## 🔄 CÁC GIAI ĐOẠN THANH TOÁN

### **Giai đoạn 1: Đặt cọc (Booking/Contract Creation)**

```
Thời điểm: Khi khách đặt xe/ký hợp đồng
Action:
  ├─ Áp dụng DepositPolicy theo VehicleType
  ├─ Tính tiền cọc: CalculationType = Percentage → 30% RentalAmount
  │                 hoặc FixedAmount → Số tiền cố định
  ├─ Đảm bảo: MinimumAmount ≤ Deposit ≤ MaximumAmount
  └─ Tạo PaymentTransaction:
      ├─ TxnType = Deposit
      ├─ Amount = DepositAmount
      ├─ Status = Success (sau khi khách chuyển tiền)
      └─ ContractId = xxx

Status hợp đồng: Active → AwaitingDelivery
```

**Code minh họa:**
```csharp
var depositPolicy = await GetDepositPolicyAsync(vehicleTypeId);
decimal depositAmount = depositPolicy.CalculationType == DepositCalculationType.Percentage
    ? rentalAmount * (depositPolicy.Value / 100)
    : depositPolicy.Value;

// Áp dụng min/max
depositAmount = Math.Max(depositAmount, depositPolicy.MinimumAmount);
depositAmount = Math.Min(depositAmount, depositPolicy.MaximumAmount);

contract.SnapshotDepositAmount = depositAmount;
```

---

### **Giai đoạn 2: Thanh toán tiền thuê (Handover - Nhận xe)**

```
Thời điểm: Khi giao xe cho khách
Action:
  ├─ Số tiền cần thu = RentalAmount - DepositAmount
  └─ Tạo PaymentTransaction:
      ├─ TxnType = RentalFee
      ├─ Amount = RentalAmount - DepositAmount
      ├─ Status = Success
      └─ ContractId = xxx

Status hợp đồng: AwaitingDelivery → InProgress
```

**Ví dụ:**
- Tổng tiền thuê: 5.000.000 VNĐ
- Đã cọc: 1.500.000 VNĐ
- Thu thêm lúc giao xe: **3.500.000 VNĐ**

---

### **Giai đoạn 3: Phát sinh phụ phí (Return - Trả xe)**

```
Thời điểm: Khi khách trả xe
Action:
  ├─ Kiểm tra các điều kiện phụ phí
  ├─ Tính phụ phí TỨC THÌ:
  │   ├─ Overtime: (ActualEnd - PlannedEnd) × OvertimeHourlyRate
  │   ├─ ExtraKilometer: (OdoKmIn - OdoKmOut - AllowedKm) × PerKmRate
  │   ├─ CleaningFee: Nếu xe quá bẩn
  │   └─ DeliveryService: Nếu khách yêu cầu giao xe
  │
  ├─ Tạo PaymentTransaction cho mỗi phụ phí:
  │   ├─ TxnType = Surcharge
  │   ├─ Status = Success (nếu thu ngay)
  │   │          hoặc Pending (nếu chưa thu được)
  │   └─ RefType = SurchargePolicy
  │       RefId = SurchargePolicyId
  │
  └─ Cập nhật: contract.ExtraCharges += totalSurcharges

Status hợp đồng: InProgress → AwaitingReturn → Returned (Pending Settlement)
```

**Ví dụ phụ phí tức thì:**
```
- Trả xe muộn 3 giờ: 3 × 50.000 = 150.000 VNĐ
- Quá km (350km - 300km): 50 × 5.000 = 250.000 VNĐ
- Xe bẩn cần vệ sinh: 200.000 VNĐ (cố định)
───────────────────────────────────────────────
Tổng phụ phí tức thì: 600.000 VNĐ (Thu ngay)
```

---

### **Giai đoạn 4: Phụ phí chậm (Delayed Surcharges)**

#### **4A. Phụ phí hư hỏng xe (3-7 ngày)**

```
Ngày 1: Trả xe
  └─ Phát hiện hư hỏng: "Vỡ gương, móp cản"
  └─ Tạo IncidentCost:
      ├─ Status = Pending
      ├─ EstimatedAmount = 0 (chưa biết giá)
      └─ Note = "Chờ garage báo giá"

Ngày 5: Garage báo giá
  └─ Update IncidentCost:
      ├─ ActualAmount = 1.200.000 VNĐ
      └─ Status = Confirmed

Ngày 5: Tạo PaymentTransaction
  └─ TxnType = Surcharge
  └─ Status = Pending (chưa thu)
  └─ Amount = 1.200.000 VNĐ
  └─ RefType = Incident
      RefId = IncidentId

Ngày 6: Khách xác nhận và thanh toán
  └─ Update PaymentTransaction.Status = Success
  └─ Update contract.ExtraCharges += 1.200.000
```

#### **4B. Phạt nguội giao thông (1-4 tuần)**

```
Ngày 1: Trả xe
  └─ Chưa có vi phạm gì
  └─ Giữ cọc, chưa hoàn

Ngày 20: Nhận thông báo phạt nguội từ Công an
  └─ Tạo ContractViolation:
      ├─ ViolationType = TrafficFine
      ├─ DetectedAt = Ngày 2 (trong thời gian thuê)
      ├─ PenaltyAmount = 500.000 VNĐ
      └─ Status = Open

Ngày 20: Tạo PaymentTransaction
  └─ TxnType = Penalty
  └─ Status = Pending
  └─ Amount = 500.000 VNĐ
  └─ RefType = ContractViolation
      RefId = ViolationId

Ngày 21: Thông báo khách qua Email/SMS
  └─ "Bạn có phạt nguội 500k, vui lòng thanh toán"

Ngày 25: Khách thanh toán
  └─ Update PaymentTransaction.Status = Success
  └─ Update ContractViolation.Status = Paid
```

---

### **Giai đoạn 5: Quyết toán cuối cùng (Final Settlement)**

```
Điều kiện: TẤT CẢ phụ phí đã được xác định
  ├─ Không còn Incident nào Pending
  ├─ Không còn ContractViolation nào Open
  └─ Tất cả PaymentTransaction.Status = Success

Action:
  ├─ Tính tổng phụ phí:
  │   TotalSurcharges = SUM(Amount WHERE TxnType IN (Surcharge, Penalty))
  │
  ├─ Tính số tiền hoàn:
  │   RefundAmount = SnapshotDepositAmount - TotalSurcharges
  │
  ├─ Kiểm tra điều kiện hoàn cọc (từ DepositPolicy):
  │   ├─ FullRefundCondition: 100% cọc
  │   ├─ PartialRefundCondition: 50% cọc
  │   └─ NoRefundCondition: 0% cọc
  │
  └─ Tạo PaymentTransaction:
      ├─ TxnType = RefundDeposit
      ├─ Amount = RefundAmount (có thể âm nếu phụ phí > cọc)
      ├─ Status = Success (sau khi chuyển khoản)
      └─ ContractId = xxx

Status hợp đồng: Returned → Completed
```

**Ví dụ quyết toán:**
```
Tiền cọc ban đầu:           3.000.000 VNĐ
─────────────────────────────────────────
Trừ phụ phí tức thì:         -600.000 VNĐ
Trừ chi phí sửa xe:        -1.200.000 VNĐ  
Trừ phạt nguội:              -500.000 VNĐ
─────────────────────────────────────────
Số tiền hoàn khách:          700.000 VNĐ ✓
```

**Trường hợp phụ phí > cọc:**
```
Tiền cọc ban đầu:           3.000.000 VNĐ
─────────────────────────────────────────
Trừ phụ phí tức thì:         -600.000 VNĐ
Trừ sửa xe nặng:           -4.500.000 VNĐ (tai nạn)
Trừ phạt:                    -500.000 VNĐ
─────────────────────────────────────────
Số tiền âm:                -2.600.000 VNĐ ✗

→ Không hoàn cọc
→ Khách phải trả thêm: 2.600.000 VNĐ
→ Tạo PaymentTransaction:
    TxnType = Surcharge
    Amount = 2.600.000
    Status = Pending
```

---

## 🎯 POLICY THỜI GIAN GIỮ CỌC

### **Khuyến nghị:**

| Trường hợp | Thời gian giữ cọc | Lý do |
|------------|-------------------|-------|
| **Xe không hư hỏng** | 7 ngày | Chờ phạt nguội (nếu có) |
| **Có hư hỏng nhỏ** | 14 ngày | Chờ garage báo giá + sửa chữa |
| **Tai nạn nặng** | 30 ngày | Chờ bảo hiểm + định giá thiệt hại |
| **Vi phạm nghiêm trọng** | 45 ngày | Chờ công an xử lý |

**Cấu hình trong DepositPolicy:**
```csharp
public int RefundProcessingDays { get; set; } // Số ngày xử lý tiêu chuẩn

// Nếu có Incident → Kéo dài thêm
// Nếu có Violation → Kéo dài thêm
```

---

## 💻 IMPLEMENTATION

### **Thêm trường vào RentalContract:**

```csharp
public class RentalContract
{
    // ... existing fields ...
    
    /// <summary>Trạng thái thanh toán cuối cùng</summary>
    public bool IsFinalSettled { get; set; } = false;
    
    /// <summary>Ngày quyết toán cuối cùng</summary>
    public DateTime? FinalSettledAt { get; set; }
    
    /// <summary>Ngày dự kiến hoàn cọc (DepositPolicy.RefundProcessingDays)</summary>
    public DateTime? ExpectedRefundDate { get; set; }
    
    /// <summary>Số tiền đã hoàn cọc thực tế</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualRefundAmount { get; set; }
}
```

### **Service Methods cần có:**

```csharp
// PricingService
Task<decimal> CalculateDepositAsync(Guid vehicleTypeId, decimal rentalAmount);
Task<List<SurchargePolicy>> GetApplicableSurchargesAsync(RentalContract contract);
Task<decimal> CalculateSurchargeAsync(SurchargePolicy policy, RentalContract contract, ReturnRecord returnRecord);

// PaymentService  
Task<PaymentTransaction> CreateDepositTransactionAsync(RentalContract contract);
Task<PaymentTransaction> CreateRentalFeeTransactionAsync(RentalContract contract);
Task<PaymentTransaction> CreateSurchargeTransactionAsync(RentalContract contract, SurchargeType type, decimal amount);
Task<bool> CanFinalizeContractAsync(Guid contractId); // Kiểm tra có còn phụ phí pending?
Task<PaymentTransaction> ProcessRefundDepositAsync(RentalContract contract);
```

---

## 📱 UI/UX CHO KHÁCH HÀNG

### **Màn hình "Trạng thái thanh toán":**

```
┌─────────────────────────────────────────────────┐
│  HỢP ĐỒNG #HD-001234                            │
│  Trạng thái: Đã trả xe - Chờ quyết toán        │
├─────────────────────────────────────────────────┤
│  ✓ Đã thanh toán tiền thuê:      5.000.000 đ   │
│  ✓ Đã thanh toán phụ phí tức thì:  600.000 đ   │
│  ⏳ Chờ xác định chi phí sửa xe               │
│  ⏳ Chờ thông báo phạt nguội (nếu có)          │
├─────────────────────────────────────────────────┤
│  💰 Tiền cọc đang giữ:           3.000.000 đ   │
│  📅 Dự kiến hoàn cọc: 08/02/2026 (7 ngày nữa)  │
├─────────────────────────────────────────────────┤
│  ℹ️ Chúng tôi sẽ hoàn cọc sau khi:              │
│    • Xác định xong chi phí sửa chữa (nếu có)   │
│    • Đợi hết thời gian phạt nguội (21 ngày)    │
│    • Trừ đi các phụ phí phát sinh              │
└─────────────────────────────────────────────────┘
```

---

## ⚠️ XỬ LÝ EDGE CASES

### **1. Khách không chấp nhận phụ phí**

```
Khách phản đối: "Tôi không gây hư hỏng này!"
→ Escalate lên Manager
→ Cần có ảnh/video từ HandoverRecord
→ PaymentTransaction.Status = Disputed
→ Giữ tiền cọc đến khi giải quyết xong
```

### **2. Phụ phí > Tiền cọc**

```
Cần thu thêm từ khách
→ Tạo PaymentTransaction.Status = Pending
→ Gửi hóa đơn/thông báo cho khách
→ Nếu khách không trả sau 30 ngày → Chuyển công ty thu nợ
```

### **3. Khách đòi hoàn cọc ngay**

```
Trường hợp đặc biệt: Khách cần tiền gấp
→ Cho ký giấy cam kết trả phụ phí phát sinh sau
→ Hoàn 80% cọc ngay, giữ 20% để dự phòng
→ Rủi ro: Khách có thể không trả phụ phí sau
```

---

## 🔐 BẢO MẬT & AUDIT

```csharp
// Mọi thay đổi về Payment phải log
public class PaymentAuditLog
{
    public Guid LogId { get; set; }
    public Guid TransactionId { get; set; }
    public string Action { get; set; } // Created, Updated, Cancelled, Refunded
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
    public decimal OldAmount { get; set; }
    public decimal NewAmount { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Reason { get; set; }
}
```

---

## ✅ CHECKLIST TRƯỚC KHI HOÀN CỌC

- [ ] Tất cả PaymentTransaction.Status = Success (không còn Pending)
- [ ] Không còn Incident nào ở trạng thái Pending
- [ ] Không còn ContractViolation nào ở trạng thái Open
- [ ] Đã qua thời gian chờ phạt nguội (7-21 ngày tùy policy)
- [ ] Khách hàng đã xác nhận đồng ý với các phụ phí
- [ ] Manager đã approve (nếu có tranh chấp)

---

## 📊 METRICS CẦN THEO DÕI

```sql
-- Thời gian giữ cọc trung bình
SELECT AVG(DATEDIFF(day, ActualEnd, FinalSettledAt)) as AvgDaysToSettle
FROM RentalContracts
WHERE IsFinalSettled = 1;

-- % hợp đồng có phụ phí phát sinh sau
SELECT 
    COUNT(CASE WHEN ExtraCharges > 0 THEN 1 END) * 100.0 / COUNT(*) as PctWithSurcharges
FROM RentalContracts
WHERE Status = 'Completed';

-- Tổng tiền cọc đang giữ (chưa hoàn)
SELECT SUM(SnapshotDepositAmount) as TotalDepositHeld
FROM RentalContracts
WHERE Status IN ('Returned', 'AwaitingReturn') AND IsFinalSettled = 0;
```

---

**Tổng kết:** Hệ thống sẽ giữ cọc từ 7-45 ngày tùy tình huống, đảm bảo thu đủ phụ phí trước khi hoàn tiền cho khách. Điều này bảo vệ lợi ích công ty và tránh tình trạng khách không trả phụ phí phát sinh sau.
