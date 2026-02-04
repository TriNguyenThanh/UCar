# KẾ HOẠCH TRIỂN KHAI MODULE 7: QUẢN LÝ HÓA ĐƠN (INVOICE MANAGEMENT)

> **Lưu ý:** Module này chỉ tập trung vào **Quản lý hóa đơn**. Phần **Quản lý thanh toán** đã có sẵn (PaymentController, PaymentService) và sẽ do người khác phát triển thêm.

---

## � LUỒNG NGHIỆP VỤ THỰC TẾ

### Quy trình thuê xe từ A-Z:

**1. Khách đặt xe (Booking)**
- Khách chọn ngày thuê (từ ngày A → ngày B) và loại xe
- Hệ thống tính giá **REALTIME**:
  - Phân tích số ngày thường, số ngày lễ trong khoảng thời gian
  - Tính: `TotalPrice = (NormalDays × BasePrice) + (PeakDays × PeakPrice)`
  - VD: Thuê 5 ngày (3 ngày thường + 2 ngày lễ) → Giá hiển thị ngay
- Khách chọn xe cụ thể và xác nhận đặt

**2. Tạo hợp đồng (Contract Creation)**
- Sau khi khách xác nhận → Hệ thống **sinh hợp đồng** bao gồm:
  - Thông tin khách hàng, thông tin xe, tình trạng xe
  - **BẢNG GIÁ CHI TIẾT** (SNAPSHOT tại thời điểm này):
    * X ngày thường × BasePrice = Y VNĐ
    * Z ngày lễ × PeakPrice = W VNĐ
    * Giá phụ phí vượt giờ (OT): 60.000đ/giờ
    * (Nếu thuê theo tháng: Giá tháng)
  - **QUAN TRỌNG**: Lưu snapshot giá vào database, tránh thay đổi sau này

**3. Đặt cọc (Deposit Payment) - KHI KÝ HỢP ĐỒNG**
- Nhân viên chuyển hợp đồng tới khách
- Khách thanh toán 2 khoản cọc:
  - **Cọc trách nhiệm**: **CỐ ĐỊNH 2.000.000 VNĐ** (không phụ thuộc thời gian thuê)
  - **Cọc thuê xe**: **50% giá trị hợp đồng** (để đảm bảo khách không bùng lịch)
- 💡 **Sinh Invoice #1: Deposit Invoice**

**4. Nhận xe (Handover) - THANH TOÁN TIỀN THUÊ**
- Khách đến nhận xe
- Khách thanh toán **TIỀN THUÊ XE** (phần còn lại sau khi trừ cọc)
- Ví dụ:
  - Tổng giá hợp đồng: 5.000.000 VNĐ
  - Cọc thuê xe đã trả: 2.500.000 VNĐ (50%)
  - Cọc trách nhiệm: 2.000.000 VNĐ (riêng biệt)
  - **Tiền thuê cần trả lúc nhận xe: 2.500.000 VNĐ**
- 💡 **Sinh Invoice #2: Rental Payment Invoice**

**5. Trả xe (Return) - THANH TOÁN PHỤ PHÍ/PHẠT (nếu có)**
- Khách trả xe
- Nhân viên kiểm tra:
  - Vượt giờ? → Phụ phí OT
  - Vượt km? → Phụ phí km
  - Xe bẩn? → Phí vệ sinh
  - Thiếu xăng? → Phí nhiên liệu
  - Vi phạm hợp đồng? → Phạt
- Khách thanh toán các phụ phí/phạt (nếu có)
- 💡 **Sinh Invoice #3: Surcharge/Penalty Invoice** (nếu có phát sinh)

**6. Hoàn cọc (Refund) - SAU 15-30 NGÀY**
- Sau 15-30 ngày kể từ khi trả xe
- Hệ thống hoàn lại:
  - **Cọc trách nhiệm**: 2.000.000 VNĐ (nếu không vi phạm)
  - **Cọc thuê xe**: 2.500.000 VNĐ (đã trả trước)
- Trừ đi các khoản phạt/phụ phí chưa thanh toán (nếu có)
- 💡 **Sinh Invoice #4: Refund Invoice**

---

## �📋 PHÂN TÍCH HIỆN TRẠNG HỆ THỐNG

### 1. Database Models Hiện Có

#### RentalContract (Hợp đồng thuê)
```csharp
- ContractId (Guid)
- ContractCode (string) - Mã hợp đồng: HD-2601-0001
- CustomerId, VehicleId, PriceId
- RentalAmount (decimal) - Tiền thuê xe
- ExtraCharges (decimal) - Phụ phí
- TotalAmountFinal (decimal) - Tổng tiền cuối cùng

// SNAPSHOT PRICING (từ Module 3 - Đã có sẵn)
- SnapshotBaseDailyPrice (decimal) - Giá ngày thường tại thời điểm ký
- SnapshotMonthMultiplier (decimal) - Hệ số tháng
- SnapshotPeakMultiplier (decimal) - Hệ số lễ
- SnapshotOvertimeHourlyPrice (decimal) - Giá vượt giờ
- NormalDays, PeakDays, NormalDaysAmount, PeakDaysAmount
- IsMonthlyRate, MonthlyAmount

// DEPOSIT SNAPSHOT (từ DepositPolicy - Module 3)
- ResponsibilityDeposit (decimal) - Cọc trách nhiệm (từ DepositPolicy.ResponsibilityDepositAmount)
- RentalDeposit (decimal) - Cọc thuê xe (tính từ DepositPolicy.RentalDepositValue)
- DepositRefundDueDate (DateTime?) - Ngày hoàn cọc (ReturnedAt + RefundProcessingDays)

- Status (RentalContractStatus)
- PlannedStart/PlannedEnd, ActualStart/ActualEnd
- Relations: PaymentTransactions, Charges, Violations, Incidents, PriceBreakdown
```

#### **DepositPolicy (Chính sách đặt cọc - MODULE 3 ĐÃ CÓ)**
```csharp
- DepositPolicyId (Guid)
- VehicleTypeId (Guid)
- PolicyName (string)
- ResponsibilityDepositAmount (decimal) - CỐ ĐỊNH cho mỗi loại xe (VD: 2.000.000 VNĐ)
- RentalDepositCalculationType (DepositCalculationType) - Percentage/FixedAmount
- RentalDepositValue (decimal) - Giá trị % (VD: 50) hoặc cố định
- RentalDepositMinimum (decimal) - Giới hạn min
- RentalDepositMaximum (decimal) - Giới hạn max
- RefundProcessingDays (int) - Số ngày xử lý hoàn cọc (15-30 ngày)
- IsActive (bool)
```

#### **SurchargePolicy (Chính sách phụ phí - MODULE 3 ĐÃ CÓ)**
```csharp
- SurchargePolicyId (Guid)
- VehicleTypeId (Guid)
- PolicyName (string)
- SurchargeType (SurchargeType) - Overtime, ExtraKilometer, CleaningFee, FuelShortage, etc.
- CalculationType (SurchargeCalculationType) - FixedAmount, Percentage, PerUnit
- Value (decimal) - Giá trị phụ phí
- AppliesTo (string) - "Rental" hoặc "Return"
- Unit (string?) - "giờ", "km", "lần"...
- ValidFrom, ValidUntil, IsActive
```

#### **Price (Bảng giá - MODULE 3 ĐÃ CÓ)**
```csharp
- PriceId (Guid)
- VehicleTypeId (Guid)
- BaseDailyPrice (decimal) - Giá ngày thường
- MonthlyMultiplier (decimal) - Hệ số tháng
- PeakDayMultiplier (decimal) - Hệ số lễ
- OvertimeHourlyPrice (decimal) - Giá vượt giờ
- IsActive (bool)
```

#### **ContractPriceBreakdown (Chi tiết bảng giá snapshot - MODULE 3 ĐÃ CÓ)**
```csharp
- BreakdownId (Guid)
- ContractId (Guid)
- PriceType (PriceBreakdownType) - Base, Peak, Monthly, Overtime
- Days (int) - Số ngày
- UnitPrice (decimal) - Đơn giá
- TotalAmount (decimal) - Thành tiền
```

#### PaymentTransaction (Giao dịch thanh toán)
```csharp
- TxnId (Guid)
- ContractId (Guid?)
- CustomerId (Guid)
- TxnType (TransactionType) - ResponsibilityDeposit, RentalDeposit, RentalFee, Penalty, RefundResponsibility, RefundRental
- Amount (decimal)
- PaymentMethod (PaymentMethod) - Cash, Transfer, Card
- BankRefCode (string?)
- PaidAt (DateTime)
- Status (TransactionStatus)
- RefType (ReferenceType?) - RefId (Guid?)
- Note (string?)
```

#### ContractCharge (Phí phát sinh trên hợp đồng)
```csharp
- ChargeId (Guid)
- ContractId (Guid)
- ViolationId (Guid?)
- ChargeType (ChargeType)
- Amount (decimal)
- Description (string?)
- IsPaid (bool)
- CreatedAt (DateTime)
```

### 2. Controllers & Services Hiện Có

**PaymentController** - Đã có sẵn:
- `GET Payment(contractId)` - Trang thanh toán cho hợp đồng
- `POST ProcessPayment()` - Xử lý thanh toán
- `GET History()` - Lịch sử thanh toán
- `POST Refund()` - Hoàn tiền

**ContractController** - Đã có sẵn:
- `GET Print(id)` - In hợp đồng (có thể tham khảo cho in hóa đơn)
- Hiển thị chi tiết hợp đồng, charges, violations

**PaymentService** - Đã có sẵn:
- `GetPaymentInfoAsync()` - Lấy thông tin thanh toán
- `ProcessPaymentAsync()` - Xử lý thanh toán
- `GetPaymentHistoryAsync()` - Lấy lịch sử thanh toán

### 3. Views Hiện Có

- `Views/Payment/Payment.cshtml` - Form thanh toán
- `Views/Payment/History.cshtml` - Lịch sử giao dịch
- `Views/Contract/Print.cshtml` - Template in hợp đồng (có thể tham khảo)
- `Views/Handover/HandoverDocument.cshtml` - Biên bản giao xe (template tham khảo)

---

## 🎯 MỤC TIÊU MODULE HÓA ĐƠN

### Chức năng chính theo ĐÚNG FLOW:

**Quản lý 4 loại hóa đơn theo giai đoạn:**

1. **Deposit Invoice (Hóa đơn đặt cọc)**
   - Phát sinh: Khi ký hợp đồng (Status = Active)
   - Nội dung (lấy từ **DepositPolicy** - Module 3):
     * Cọc trách nhiệm: Theo `DepositPolicy.ResponsibilityDepositAmount` (VD: 2.000.000 VNĐ cho Sedan)
     * Cọc thuê xe: Tính theo `DepositPolicy.RentalDepositValue` (VD: 50% giá trị hợp đồng)
   - Trạng thái: Unpaid → Paid
   - ⚠️ **Sử dụng DepositPolicy đã cấu hình, KHÔNG hardcode**

2. **Rental Invoice (Hóa đơn tiền thuê)**
   - Phát sinh: Khi nhận xe (handover)
   - Nội dung (sử dụng **Snapshot Pricing** từ Contract - Module 3):
     * Tiền thuê xe (= Tổng giá hợp đồng - Cọc thuê xe đã trả)
     * Chi tiết từ `ContractPriceBreakdown`:
       - X ngày thường × SnapshotBaseDailyPrice
       - Y ngày lễ × (BaseDailyPrice × SnapshotPeakMultiplier)
       - Thuê tháng × MonthlyAmount (nếu có)
     * Trừ: Cọc thuê xe đã thanh toán
   - Trạng thái: Unpaid → Paid
   - ⚠️ **Hiển thị chi tiết từ ContractPriceBreakdown, KHÔNG tính lại giá**

3. **Surcharge/Penalty Invoice (Hóa đơn phụ phí/phạt)**
   - Phát sinh: Khi trả xe (return) - NẾU CÓ phát sinh
   - Nội dung (từ **ContractCharge** + **SurchargePolicy** - Module 3):
     * Phụ phí vượt giờ: Sử dụng `Contract.SnapshotOvertimeHourlyPrice`
     * Phụ phí vượt km: Tìm `SurchargePolicy` (ExtraKilometer) theo VehicleType
     * Phí vệ sinh: Tìm `SurchargePolicy` (CleaningFee) theo VehicleType
     * Phí thiếu nhiên liệu: Tìm `SurchargePolicy` (FuelShortage) theo VehicleType
     * Phạt vi phạm hợp đồng: Từ `ContractViolation` → `ContractCharge`
   - Trạng thái: Unpaid → Paid
   - ⚠️ **Sử dụng SurchargePolicy đã cấu hình cho từng loại phí**

4. **Refund Invoice (Hóa đơn hoàn cọc)**
   - Phát sinh: Sau `DepositPolicy.RefundProcessingDays` ngày từ khi trả xe (VD: 15-30 ngày)
   - Nội dung (từ **Contract** + **DepositPolicy**):
     * Hoàn cọc trách nhiệm: `Contract.ResponsibilityDeposit`
     * Hoàn cọc thuê xe: `Contract.RentalDeposit`
     * Trừ đi: Tổng `ContractCharge` WHERE `IsPaid = false`
   - Trạng thái: Pending → Refunded
   - ⚠️ **Thời gian hoàn cọc theo DepositPolicy.RefundProcessingDays**

### Tính năng hỗ trợ:
- ✅ Tạo hóa đơn tự động cho từng giai đoạn
- ✅ Theo dõi trạng thái thanh toán từng hóa đơn
- ✅ Xem tổng quan tất cả hóa đơn của 1 hợp đồng
- ✅ In/Xuất PDF hóa đơn
- ✅ Gửi email hóa đơn cho khách hàng
- ✅ Báo cáo doanh thu theo từng loại hóa đơn

---

## 🗄️ THIẾT KẾ DATABASE

### Bảng: Invoice (Hóa đơn)

```csharp
public class Invoice
{
    [Key]
    public Guid InvoiceId { get; set; }

    /// <summary>Mã hóa đơn: INV-2601-0001</summary>
    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Hợp đồng liên quan</summary>
    [Required]
    public Guid ContractId { get; set; }

    /// <summary>Khách hàng</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Loại hóa đơn</summary>
    [Required]
    public InvoiceType InvoiceType { get; set; }

    /// <summary>Ngày phát hành hóa đơn</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Ngày đến hạn thanh toán</summary>
    public DateTime? DueDate { get; set; }

    // ===== CHI TIẾT SỐ TIỀN =====

    /// <summary>Tiền thuê xe cơ bản</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseRentalAmount { get; set; }

    /// <summary>Tổng phụ phí (overtime, extra km, cleaning...)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SurchargesTotal { get; set; }

    /// <summary>Tổng phí phạt (vi phạm, sự cố...)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PenaltiesTotal { get; set; }

    /// <summary>Các khoản giảm trừ (khuyến mãi, voucher...)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    /// <summary>Thuế VAT (nếu có)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>Tiền cọc đã trả (trừ vào tổng)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositPaid { get; set; }

    /// <summary>Tổng tiền hóa đơn (sau giảm trừ, thuế)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Số tiền đã thanh toán</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    /// <summary>Số tiền còn phải trả</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountDue { get; set; }

    // ===== TRẠNG THÁI =====

    [Required]
    public InvoiceStatus Status { get; set; }

    /// <summary>Ghi chú trên hóa đơn</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Ghi chú nội bộ (không in ra hóa đơn)</summary>
    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    // ===== AUDIT =====

    /// <summary>Nhân viên phát hành hóa đơn</summary>
    [Required]
    public Guid IssuedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Người hủy hóa đơn (nếu có)</summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>Thời gian hủy</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Lý do hủy</summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContractId))]
    public RentalContract Contract { get; set; } = null!;

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(IssuedBy))]
    public UserAccount Issuer { get; set; } = null!;

    [ForeignKey(nameof(CancelledBy))]
    public UserAccount? Canceller { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<InvoiceAdjustment> Adjustments { get; set; } = new List<InvoiceAdjustment>();
}
```

### Bảng: InvoiceLineItem (Các kстатья chi tiết trên hóa đơn)

```csharp
public class InvoiceLineItem
{
    [Key]
    public Guid LineItemId { get; set; }

    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>Loại mục: Rental, Surcharge, Penalty, Deposit, Refund</summary>
    [Required]
    public LineItemType ItemType { get; set; }

    /// <summary>Mô tả mục (VD: "Thuê xe 5 ngày", "Phụ phí quá giờ 2h")</summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Số lượng (VD: 5 ngày, 2 giờ, 50 km...)</summary>
    public decimal Quantity { get; set; }

    /// <summary>Đơn vị (ngày, giờ, km, lần...)</summary>
    [MaxLength(50)]
    public string? Unit { get; set; }

    /// <summary>Đơn giá</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Thành tiền (Quantity × UnitPrice)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Tham chiếu đến phụ phí, phạt... (nếu có)</summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>Loại tham chiếu (ContractCharge, SurchargePolicy...)</summary>
    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;
}
```

### Bảng: InvoiceAdjustment (Lịch sử điều chỉnh hóa đơn)

```csharp
public class InvoiceAdjustment
{
    [Key]
    public Guid AdjustmentId { get; set; }

    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>Loại điều chỉnh: Discount, AdditionalCharge, Correction, Cancellation</summary>
    [Required]
    public AdjustmentType AdjustmentType { get; set; }

    /// <summary>Số tiền điều chỉnh (+ hoặc -)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Lý do điều chỉnh</summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Ghi chú bổ sung</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Nhân viên thực hiện điều chỉnh</summary>
    [Required]
    public Guid AdjustedBy { get; set; }

    public DateTime AdjustedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(AdjustedBy))]
    public UserAccount Adjuster { get; set; } = null!;
}
```

---

## 📊 ENUMS

### InvoiceType (Loại hóa đơn)
```csharp
public enum InvoiceType
{
    Deposit,         // Hóa đơn đặt cọc (khi ký hợp đồng)
    Rental,          // Hóa đơn tiền thuê (khi nhận xe)
    Surcharge,       // Hóa đơn phụ phí (vượt giờ, vượt km, vệ sinh...)
    Penalty,         // Hóa đơn phạt (vi phạm hợp đồng)
    Refund           // Hóa đơn hoàn cọc (sau 15-30 ngày)
}
```

### InvoiceStatus (Trạng thái hóa đơn)
```csharp
public enum InvoiceStatus
{
    Draft,           // Nháp (chưa phát hành)
    Issued,          // Đã phát hành (chờ thanh toán)
    PartiallyPaid,   // Đã thanh toán một phần
    Paid,            // Đã thanh toán đủ
    Overdue,         // Quá hạn thanh toán
    Cancelled,       // Đã hủy
    Refunded         // Đã hoàn tiền (cho Refund Invoice)
}
```

### LineItemType (Loại mục trên hóa đơn)
```csharp
public enum LineItemType
{
    // Deposit Invoice items
    ResponsibilityDeposit,   // Cọc trách nhiệm (2M VNĐ cố định)
    RentalDeposit,           // Cọc thuê xe (50% giá trị HĐ)
    
    // Rental Invoice items
    BaseRental,              // Tiền thuê cơ bản (ngày thường)
    PeakRental,              // Tiền thuê ngày lễ
    MonthlyRental,           // Tiền thuê theo tháng
    
    // Surcharge Invoice items
    OvertimeSurcharge,       // Phụ phí vượt giờ
    ExtraKmSurcharge,        // Phụ phí vượt km
    CleaningSurcharge,       // Phí vệ sinh
    FuelSurcharge,           // Phí thiếu nhiên liệu
    
    // Penalty Invoice items
    ContractViolation,       // Phạt vi phạm hợp đồng
    DamagePenalty,           // Phạt hư hỏng xe
    
    // Refund Invoice items
    RefundResponsibility,    // Hoàn cọc trách nhiệm
    RefundRental,            // Hoàn cọc thuê xe
    
    // Other
    Discount,                // Giảm giá
    Tax,                     // Thuế
    Other                    // Khác
}
```

### AdjustmentType (Loại điều chỉnh)
```csharp
public enum AdjustmentType
{
    Discount,            // Giảm giá
    AdditionalCharge,    // Phụ phí bổ sung
    Correction,          // Điều chỉnh sai sót
    Cancellation         // Hủy bỏ
}
```

---

## 🔧 INTERFACES & SERVICES

### IInvoiceService

```csharp
public interface IInvoiceService
{
    // Tạo hóa đơn
    Task<Guid> CreateInvoiceFromContractAsync(Guid contractId, Guid issuedBy);
    Task<Guid> CreateManualInvoiceAsync(InvoiceCreateViewModel model, Guid issuedBy);

    // Quản lý hóa đơn
    Task<PaginatedResult<InvoiceListItemViewModel>> GetInvoicesAsync(
        InvoiceStatus? status, 
        InvoiceType? type,
        DateTime? fromDate,
        DateTime? toDate,
        string? searchTerm,
        int page,
        int pageSize);

    Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(Guid invoiceId);
    Task<InvoicePrintViewModel?> GetInvoiceForPrintAsync(Guid invoiceId);

    // Cập nhật
    Task UpdateInvoiceAsync(InvoiceEditViewModel model, Guid updatedBy);
    Task UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus newStatus, Guid updatedBy);
    Task RecordPaymentAsync(Guid invoiceId, decimal amount, Guid txnId);

    // Điều chỉnh
    Task<Guid> AddAdjustmentAsync(InvoiceAdjustmentViewModel model, Guid adjustedBy);
    Task CancelInvoiceAsync(Guid invoiceId, string reason, Guid cancelledBy);

    // Tính toán
    Task<InvoiceCalculationResult> CalculateInvoiceAmountsAsync(Guid contractId);
    decimal CalculateTotalAmount(decimal baseRental, decimal surcharges, decimal penalties, decimal discount, decimal tax);

    // Xuất hóa đơn
    Task<byte[]> ExportInvoiceToPdfAsync(Guid invoiceId);
    Task SendInvoiceByEmailAsync(Guid invoiceId, string recipientEmail);

    // Thống kê
    Task<InvoiceStatisticsViewModel> GetInvoiceStatisticsAsync(DateTime fromDate, DateTime toDate);
}
```

---

## 📱 VIEWMODELS

### InvoiceListItemViewModel
```csharp
public class InvoiceListItemViewModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ContractCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public bool IsOverdue { get; set; }
}
```

### InvoiceDetailsViewModel
```csharp
public class InvoiceDetailsViewModel
{
    // Thông tin cơ bản
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Khách hàng & Hợp đồng
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // Thông tin xe
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;

    // Chi tiết số tiền
    public decimal BaseRentalAmount { get; set; }
    public decimal SurchargesTotal { get; set; }
    public decimal PenaltiesTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DepositPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }

    // Line Items
    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    // Adjustments
    public List<InvoiceAdjustmentViewModel> Adjustments { get; set; } = new();

    // Ghi chú
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // Audit
    public string IssuedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### InvoiceCreateViewModel
```csharp
public class InvoiceCreateViewModel
{
    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public InvoiceType InvoiceType { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    // Điều chỉnh thủ công (nếu cần)
    public decimal? CustomDiscountAmount { get; set; }
    public decimal? CustomTaxAmount { get; set; }
}
```

### InvoicePrintViewModel (Cho in hóa đơn)
```csharp
public class InvoicePrintViewModel
{
    // Invoice Info
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceTypeText { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Company Info (Bên phát hành)
    public string CompanyName { get; set; } = "UCar - Hệ thống cho thuê xe";
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyTaxCode { get; set; } = string.Empty;

    // Customer Info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerTaxCode { get; set; }

    // Contract Info
    public string ContractCode { get; set; } = string.Empty;
    public string VehiclePlateNo { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public DateTime RentalStart { get; set; }
    public DateTime RentalEnd { get; set; }
    public int RentalDays { get; set; }

    // Line Items
    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }

    // Issuer
    public string IssuedByName { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }

    // Notes
    public string? Notes { get; set; }
}
```

---

## 🎨 CONTROLLERS

### InvoiceController

```csharp
[Authorize(Roles = "Admin,BranchManager,Staff")]
public class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;

    // GET: Invoice
    public async Task<IActionResult> Index(
        InvoiceStatus? status, 
        InvoiceType? type,
        DateTime? fromDate,
        DateTime? toDate,
        string? searchTerm,
        int page = 1)
    {
        var result = await _invoiceService.GetInvoicesAsync(
            status, type, fromDate, toDate, searchTerm, page, 15);
        
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentType = type;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = result.TotalPages;
        
        return View(result.Items);
    }

    // GET: Invoice/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _invoiceService.GetInvoiceDetailsAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hóa đơn";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Invoice/Create?contractId=...
    public async Task<IActionResult> Create(Guid contractId)
    {
        // Load contract info, pre-fill form
        return View(new InvoiceCreateViewModel { ContractId = contractId });
    }

    // POST: Invoice/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var userId = GetCurrentUserId();
            var invoiceId = await _invoiceService.CreateManualInvoiceAsync(model, userId);
            
            TempData["Success"] = "Đã tạo hóa đơn thành công";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    // GET: Invoice/Print/5
    public async Task<IActionResult> Print(Guid id)
    {
        var model = await _invoiceService.GetInvoiceForPrintAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hóa đơn hoặc hóa đơn chưa sẵn sàng in";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // POST: Invoice/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,BranchManager")]
    public async Task<IActionResult> Cancel(Guid id, string reason)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _invoiceService.CancelInvoiceAsync(id, reason, userId);
            
            TempData["Success"] = "Đã hủy hóa đơn";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // GET: Invoice/ExportPdf/5
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        try
        {
            var pdfBytes = await _invoiceService.ExportInvoiceToPdfAsync(id);
            var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);
            
            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // POST: Invoice/SendEmail/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(Guid id, string email)
    {
        try
        {
            await _invoiceService.SendInvoiceByEmailAsync(id, email);
            TempData["Success"] = $"Đã gửi hóa đơn đến {email}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // GET: Invoice/Statistics
    [Authorize(Roles = "Admin,BranchManager")]
    public async Task<IActionResult> Statistics(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddMonths(-1);
        var to = toDate ?? DateTime.Today;
        
        var stats = await _invoiceService.GetInvoiceStatisticsAsync(from, to);
        
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        
        return View(stats);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
```

---

## 📄 VIEWS CẦN TẠO

### 1. Views/Invoice/Index.cshtml
- Danh sách hóa đơn dạng table
- Filter: Trạng thái, Loại, Ngày, Tìm kiếm (mã HĐ, mã hợp đồng, tên KH)
- Pagination
- Highlight hóa đơn quá hạn (màu đỏ)
- Actions: Xem chi tiết, In, Xuất PDF, Hủy

### 2. Views/Invoice/Details.cshtml
- Thông tin hóa đơn đầy đủ
- Thông tin khách hàng, hợp đồng, xe
- Bảng Line Items chi tiết
- Lịch sử Adjustments
- Lịch sử thanh toán liên quan
- Actions: In, Xuất PDF, Gửi email, Hủy (nếu có quyền)

### 3. Views/Invoice/Create.cshtml
- Form tạo hóa đơn thủ công
- Chọn hợp đồng (dropdown hoặc search)
- Loại hóa đơn
- Ngày đến hạn
- Ghi chú
- Preview tính toán (AJAX load từ contract)

### 4. Views/Invoice/Print.cshtml
- Template in hóa đơn chuyên nghiệp
- Logo công ty
- Thông tin 2 bên (Công ty - Khách hàng)
- Bảng Line Items
- Tổng tiền, thuế, giảm giá
- Chữ ký số/điện tử
- CSS print-friendly

### 5. Views/Invoice/Statistics.cshtml
- Biểu đồ doanh thu từ hóa đơn (Chart.js)
- Tổng hợp: Tổng HĐ, Đã thanh toán, Quá hạn, Hủy
- Danh sách top khách hàng có doanh thu cao
- Export Excel

---

## 🔄 LUỒNG NGHIỆP VỤ CHI TIẾT

### 1. Tạo Deposit Invoice - KHI KÝ HỢP ĐỒNG

**Trigger:** Khi `RentalContract.Status` chuyển sang `Active` (sau khi khách xác nhận đặt xe)

```csharp
// Trong ContractService.ActivateContractAsync()
public async Task ActivateContractAsync(Guid contractId, Guid activatedBy)
{
    var contract = await _context.RentalContracts.FindAsync(contractId);
    contract.Status = RentalContractStatus.Active;
    
    await _context.SaveChangesAsync();
    
    // TẠO DEPOSIT INVOICE TỰ ĐỘNG
    await _invoiceService.CreateDepositInvoiceAsync(contractId, activatedBy);
}
```

**Nội dung Deposit Invoice:**
```csharp
public async Task<Guid> CreateDepositInvoiceAsync(Guid contractId, Guid issuedBy)
{
    var contract = await _context.RentalContracts
        .Include(c => c.Customer)
        .FirstOrDefaultAsync(c => c.ContractId == contractId);
    
    // Tính tiền cọc
    var responsibilityDeposit = 2_000_000m; // Cố định 2M
    var rentalDeposit = contract.TotalAmountFinal * 0.5m; // 50%
    var totalDeposit = responsibilityDeposit + rentalDeposit;
    
    var invoice = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        InvoiceNumber = await GenerateInvoiceNumberAsync("DEP"),
        ContractId = contractId,
        CustomerId = contract.CustomerId,
        InvoiceType = InvoiceType.Deposit,
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
        Description = "Cọc trách nhiệm (cố định)",
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
        Description = $"Cọc thuê xe (50% giá trị hợp đồng {contract.ContractCode})",
        Quantity = 1,
        Unit = "lần",
        UnitPrice = rentalDeposit,
        Amount = rentalDeposit,
        CreatedAt = DateTime.Now
    });
    
    await _context.SaveChangesAsync();
    
    return invoice.InvoiceId;
}
```

---

### 2. Tạo Rental Invoice - KHI NHẬN XE (HANDOVER)

**Trigger:** Khi `HandoverRecord` được tạo (khách đến nhận xe)

```csharp
// Trong HandoverService.CreateHandoverRecordAsync()
public async Task<Guid> CreateHandoverRecordAsync(HandoverCreateViewModel model, Guid handedBy)
{
    // ... Tạo HandoverRecord ...
    
    var handover = new HandoverRecord
    {
        HandoverId = Guid.NewGuid(),
        ContractId = model.ContractId,
        HandedAt = DateTime.Now,
        HandedBy = handedBy,
        // ... other fields ...
    };
    
    _context.HandoverRecords.Add(handover);
    
    // Update contract status
    var contract = await _context.RentalContracts.FindAsync(model.ContractId);
    contract.Status = RentalContractStatus.InProgress;
    contract.ActualStart = DateTime.Now;
    
    await _context.SaveChangesAsync();
    
    // TẠO RENTAL INVOICE TỰ ĐỘNG
    await _invoiceService.CreateRentalInvoiceAsync(model.ContractId, handedBy);
    
    return handover.HandoverId;
}
```

**Nội dung Rental Invoice:**
```csharp
public async Task<Guid> CreateRentalInvoiceAsync(Guid contractId, Guid issuedBy)
{
    var contract = await _context.RentalContracts
        .Include(c => c.Customer)
        .Include(c => c.PriceBreakdown)
        .FirstOrDefaultAsync(c => c.ContractId == contractId);
    
    // Lấy thông tin từ snapshot pricing
    var baseRentalAmount = contract.RentalAmount;
    
    // Tính số tiền cần thanh toán (trừ đi cọc thuê xe đã trả)
    var rentalDepositPaid = contract.RentalDeposit; // 50% đã trả
    var amountDue = baseRentalAmount - rentalDepositPaid;
    
    var invoice = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        InvoiceNumber = await GenerateInvoiceNumberAsync("RNT"),
        ContractId = contractId,
        CustomerId = contract.CustomerId,
        InvoiceType = InvoiceType.Rental,
        IssuedDate = DateTime.Now,
        DueDate = DateTime.Now, // Phải trả ngay
        
        BaseRentalAmount = baseRentalAmount,
        SurchargesTotal = 0,
        PenaltiesTotal = 0,
        DiscountAmount = 0,
        TaxAmount = 0,
        DepositPaid = rentalDepositPaid,
        
        TotalAmount = baseRentalAmount,
        AmountPaid = rentalDepositPaid, // Đã trả cọc
        AmountDue = amountDue,
        
        Status = InvoiceStatus.Issued,
        IssuedBy = issuedBy,
        CreatedAt = DateTime.Now
    };
    
    _context.Invoices.Add(invoice);
    
    // Tạo Line Items từ PriceBreakdown (snapshot)
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
    
    if (contract.PeakDays > 0)
    {
        invoice.LineItems.Add(new InvoiceLineItem
        {
            LineItemId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            ItemType = LineItemType.PeakRental,
            Description = $"Thuê xe ngày lễ",
            Quantity = contract.PeakDays,
            Unit = "ngày",
            UnitPrice = contract.SnapshotBaseDailyPrice * contract.SnapshotPeakMultiplier,
            Amount = contract.PeakDaysAmount,
            CreatedAt = DateTime.Now
        });
    }
    
    if (contract.IsMonthlyRate)
    {
        invoice.LineItems.Add(new InvoiceLineItem
        {
            LineItemId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            ItemType = LineItemType.MonthlyRental,
            Description = $"Thuê xe theo tháng",
            Quantity = contract.RentalDays / 30,
            Unit = "tháng",
            UnitPrice = contract.SnapshotBaseDailyPrice * 30 * contract.SnapshotMonthMultiplier,
            Amount = contract.MonthlyAmount,
            CreatedAt = DateTime.Now
        });
    }
    
    // Line item: Trừ cọc thuê xe
    invoice.LineItems.Add(new InvoiceLineItem
    {
        LineItemId = Guid.NewGuid(),
        InvoiceId = invoice.InvoiceId,
        ItemType = LineItemType.RentalDeposit,
        Description = "Trừ cọc thuê xe đã thanh toán",
        Quantity = 1,
        Unit = "lần",
        UnitPrice = -rentalDepositPaid, // Số âm
        Amount = -rentalDepositPaid,
        CreatedAt = DateTime.Now
    });
    
    await _context.SaveChangesAsync();
    
    return invoice.InvoiceId;
}
```

---

### 3. Tạo Surcharge/Penalty Invoice - KHI TRẢ XE (NẾU CÓ PHÁT SINH)

**Trigger:** Khi `ReturnRecord` được tạo VÀ có phát sinh `ContractCharge`

```csharp
// Trong ReturnService.ProcessReturnAsync()
public async Task ProcessReturnAsync(Guid contractId, ReturnViewModel model, Guid processedBy)
{
    // ... Tạo ReturnRecord ...
    
    var returnRecord = new ReturnRecord
    {
        ReturnId = Guid.NewGuid(),
        ContractId = contractId,
        ReturnedAt = DateTime.Now,
        ReceivedBy = processedBy,
        // ... other fields ...
    };
    
    _context.ReturnRecords.Add(returnRecord);
    
    // Tạo các ContractCharge (phụ phí, phạt)
    var charges = new List<ContractCharge>();
    
    // Vượt giờ?
    if (model.OvertimeHours > 0)
    {
        charges.Add(new ContractCharge
        {
            ChargeId = Guid.NewGuid(),
            ContractId = contractId,
            ChargeType = ChargeType.Surcharge,
            Amount = model.OvertimeHours * contract.SnapshotOvertimeHourlyPrice,
            Description = $"Phụ phí vượt giờ: {model.OvertimeHours} giờ × {contract.SnapshotOvertimeHourlyPrice:N0}đ/giờ",
            IsPaid = false,
            CreatedAt = DateTime.Now
        });
    }
    
    // Vượt km?
    if (model.ExtraKm > 0)
    {
        var extraKmPrice = 5000m; // Lấy từ SurchargePolicy
        charges.Add(new ContractCharge
        {
            ChargeId = Guid.NewGuid(),
            ContractId = contractId,
            ChargeType = ChargeType.Surcharge,
            Amount = model.ExtraKm * extraKmPrice,
            Description = $"Phụ phí vượt km: {model.ExtraKm} km × {extraKmPrice:N0}đ/km",
            IsPaid = false,
            CreatedAt = DateTime.Now
        });
    }
    
    // Xe bẩn?
    if (model.RequiresCleaning)
    {
        charges.Add(new ContractCharge
        {
            ChargeId = Guid.NewGuid(),
            ContractId = contractId,
            ChargeType = ChargeType.Surcharge,
            Amount = 200_000m,
            Description = "Phí vệ sinh xe",
            IsPaid = false,
            CreatedAt = DateTime.Now
        });
    }
    
    // Vi phạm hợp đồng?
    if (model.HasViolation)
    {
        charges.Add(new ContractCharge
        {
            ChargeId = Guid.NewGuid(),
            ContractId = contractId,
            ChargeType = ChargeType.Penalty,
            Amount = model.ViolationAmount,
            Description = model.ViolationDescription,
            IsPaid = false,
            CreatedAt = DateTime.Now
        });
    }
    
    _context.ContractCharges.AddRange(charges);
    
    // Update contract
    var contract = await _context.RentalContracts.FindAsync(contractId);
    contract.Status = RentalContractStatus.Returned;
    contract.ActualEnd = DateTime.Now;
    contract.ExtraCharges = charges.Sum(ch => ch.Amount);
    
    await _context.SaveChangesAsync();
    
    // TẠO SURCHARGE/PENALTY INVOICE (NẾU CÓ PHÍ PHÁT SINH)
    if (charges.Any())
    {
        await _invoiceService.CreateSurchargePenaltyInvoiceAsync(contractId, processedBy);
    }
    
    // Tự động tạo Refund Invoice (pending)
    await _invoiceService.CreateRefundInvoiceAsync(contractId, processedBy);
}
```

**Nội dung Surcharge/Penalty Invoice:**
```csharp
public async Task<Guid> CreateSurchargePenaltyInvoiceAsync(Guid contractId, Guid issuedBy)
{
    var contract = await _context.RentalContracts
        .Include(c => c.Customer)
        .Include(c => c.Charges)
        .FirstOrDefaultAsync(c => c.ContractId == contractId);
    
    var charges = contract.Charges.Where(ch => !ch.IsPaid).ToList();
    
    var surchargesTotal = charges
        .Where(ch => ch.ChargeType == ChargeType.Surcharge)
        .Sum(ch => ch.Amount);
    
    var penaltiesTotal = charges
        .Where(ch => ch.ChargeType == ChargeType.Penalty)
        .Sum(ch => ch.Amount);
    
    var totalAmount = surchargesTotal + penaltiesTotal;
    
    var invoice = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        InvoiceNumber = await GenerateInvoiceNumberAsync("SUR"),
        ContractId = contractId,
        CustomerId = contract.CustomerId,
        InvoiceType = surchargesTotal > penaltiesTotal ? InvoiceType.Surcharge : InvoiceType.Penalty,
        IssuedDate = DateTime.Now,
        DueDate = DateTime.Now.AddDays(7),
        
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
    
    // Tạo Line Items từ ContractCharges
    foreach (var charge in charges)
    {
        var itemType = charge.ChargeType == ChargeType.Surcharge 
            ? DetermineLineItemTypeFromDescription(charge.Description) 
            : LineItemType.ContractViolation;
        
        invoice.LineItems.Add(new InvoiceLineItem
        {
            LineItemId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            ItemType = itemType,
            Description = charge.Description,
            Quantity = 1,
            Unit = "lần",
            UnitPrice = charge.Amount,
            Amount = charge.Amount,
            ReferenceId = charge.ChargeId,
            ReferenceType = "ContractCharge",
            CreatedAt = DateTime.Now
        });
    }
    
    await _context.SaveChangesAsync();
    
    return invoice.InvoiceId;
}

private LineItemType DetermineLineItemTypeFromDescription(string description)
{
    if (description.Contains("vượt giờ", StringComparison.OrdinalIgnoreCase))
        return LineItemType.OvertimeSurcharge;
    if (description.Contains("vượt km", StringComparison.OrdinalIgnoreCase))
        return LineItemType.ExtraKmSurcharge;
    if (description.Contains("vệ sinh", StringComparison.OrdinalIgnoreCase))
        return LineItemType.CleaningSurcharge;
    if (description.Contains("nhiên liệu", StringComparison.OrdinalIgnoreCase))
        return LineItemType.FuelSurcharge;
    
    return LineItemType.Other;
}
```

---

### 4. Tạo Refund Invoice - SAU 15-30 NGÀY TỪ KHI TRẢ XE

**Trigger:** Scheduled job chạy hàng ngày hoặc manual trigger

```csharp
// Background Service hoặc Manual Action
public async Task CreateRefundInvoiceAsync(Guid contractId, Guid issuedBy)
{
    var contract = await _context.RentalContracts
        .Include(c => c.Customer)
        .Include(c => c.Charges)
        .Include(c => c.ReturnRecord)
        .FirstOrDefaultAsync(c => c.ContractId == contractId);
    
    if (contract.ReturnRecord == null)
        throw new InvalidOperationException("Chưa trả xe, không thể tạo hóa đơn hoàn cọc");
    
    // Tổng tiền cọc đã trả
    var responsibilityDepositPaid = contract.ResponsibilityDeposit; // 2M
    var rentalDepositPaid = contract.RentalDeposit; // 50%
    var totalDepositPaid = responsibilityDepositPaid + rentalDepositPaid;
    
    // Các khoản phạt/phụ phí chưa thanh toán
    var unpaidCharges = contract.Charges
        .Where(ch => !ch.IsPaid)
        .Sum(ch => ch.Amount);
    
    // Số tiền hoàn lại = Tổng cọc - Các khoản chưa thanh toán
    var refundAmount = totalDepositPaid - unpaidCharges;
    
    var invoice = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        InvoiceNumber = await GenerateInvoiceNumberAsync("REF"),
        ContractId = contractId,
        CustomerId = contract.CustomerId,
        InvoiceType = InvoiceType.Refund,
        IssuedDate = DateTime.Now,
        DueDate = contract.ReturnRecord.ReturnedAt.AddDays(30), // Hoàn sau 30 ngày
        
        BaseRentalAmount = 0,
        SurchargesTotal = 0,
        PenaltiesTotal = 0,
        DiscountAmount = unpaidCharges, // Trừ đi các khoản chưa thanh toán
        TaxAmount = 0,
        DepositPaid = totalDepositPaid,
        
        TotalAmount = -refundAmount, // Số âm (hoàn tiền)
        AmountPaid = 0,
        AmountDue = -refundAmount,
        
        Status = InvoiceStatus.Issued,
        Notes = $"Hoàn cọc sau {(DateTime.Now - contract.ReturnRecord.ReturnedAt).Days} ngày kể từ ngày trả xe",
        IssuedBy = issuedBy,
        CreatedAt = DateTime.Now
    };
    
    _context.Invoices.Add(invoice);
    
    // Line Items: Hoàn cọc
    invoice.LineItems.Add(new InvoiceLineItem
    {
        LineItemId = Guid.NewGuid(),
        InvoiceId = invoice.InvoiceId,
        ItemType = LineItemType.RefundResponsibility,
        Description = "Hoàn cọc trách nhiệm",
        Quantity = 1,
        Unit = "lần",
        UnitPrice = responsibilityDepositPaid,
        Amount = responsibilityDepositPaid,
        CreatedAt = DateTime.Now
    });
    
    invoice.LineItems.Add(new InvoiceLineItem
    {
        LineItemId = Guid.NewGuid(),
        InvoiceId = invoice.InvoiceId,
        ItemType = LineItemType.RefundRental,
        Description = "Hoàn cọc thuê xe",
        Quantity = 1,
        Unit = "lần",
        UnitPrice = rentalDepositPaid,
        Amount = rentalDepositPaid,
        CreatedAt = DateTime.Now
    });
    
    // Trừ đi các khoản chưa thanh toán (nếu có)
    if (unpaidCharges > 0)
    {
        invoice.LineItems.Add(new InvoiceLineItem
        {
            LineItemId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            ItemType = LineItemType.Other,
            Description = "Trừ các khoản phạt/phụ phí chưa thanh toán",
            Quantity = 1,
            Unit = "lần",
            UnitPrice = -unpaidCharges, // Số âm
            Amount = -unpaidCharges,
            CreatedAt = DateTime.Now
        });
    }
    
    await _context.SaveChangesAsync();
    
    return invoice.InvoiceId;
}
```

---

### 5. Cập nhật trạng thái Invoice khi thanh toán

### 5. Cập nhật trạng thái Invoice khi thanh toán

**Trigger:** Khi `PaymentTransaction` được tạo mới

```csharp
// Trong PaymentService.ProcessPaymentAsync()
public async Task ProcessPaymentAsync(PaymentProcessViewModel model, Guid processedBy)
{
    // ... Xử lý thanh toán ...
    
    var txn = new PaymentTransaction
    {
        TxnId = Guid.NewGuid(),
        ContractId = model.ContractId,
        CustomerId = model.CustomerId,
        Amount = model.PaymentAmount,
        TxnType = DetermineTransactionType(model.InvoiceType),
        PaymentMethod = model.PaymentMethod,
        PaidAt = DateTime.Now,
        Status = TransactionStatus.Success
    };
    
    _context.PaymentTransactions.Add(txn);
    await _context.SaveChangesAsync();
    
    // CẬP NHẬT INVOICE
    if (model.InvoiceId.HasValue)
    {
        await _invoiceService.RecordPaymentAsync(model.InvoiceId.Value, model.PaymentAmount, txn.TxnId);
    }
}

// Trong InvoiceService
public async Task RecordPaymentAsync(Guid invoiceId, decimal amount, Guid txnId)
{
    var invoice = await _context.Invoices.FindAsync(invoiceId);
    if (invoice == null)
        throw new InvalidOperationException("Invoice not found");
    
    invoice.AmountPaid += amount;
    invoice.AmountDue = invoice.TotalAmount - invoice.AmountPaid;
    invoice.UpdatedAt = DateTime.Now;
    
    // Cập nhật status
    if (invoice.AmountDue <= 0)
    {
        invoice.Status = invoice.InvoiceType == InvoiceType.Refund 
            ? InvoiceStatus.Refunded 
            : InvoiceStatus.Paid;
    }
    else if (invoice.AmountPaid > 0)
    {
        invoice.Status = InvoiceStatus.PartiallyPaid;
    }
    
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Recorded payment {Amount} for invoice {InvoiceNumber}", 
        amount, invoice.InvoiceNumber);
}
```

---

### 6. Scheduled Job: Kiểm tra và tạo Refund Invoice

**Chạy hàng ngày:**

```csharp
public class RefundInvoiceBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndCreateRefundInvoicesAsync();
            
            // Chạy mỗi ngày lúc 00:00
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
    
    private async Task CheckAndCreateRefundInvoicesAsync()
    {
        var today = DateTime.Today;
        
        // Lấy các hợp đồng đã trả xe và chưa có Refund Invoice
        var contractsForRefund = await _context.RentalContracts
            .Include(c => c.ReturnRecord)
            .Include(c => c.Invoices)
            .Where(c => c.Status == RentalContractStatus.Returned
                     && c.ReturnRecord != null
                     && c.ReturnRecord.ReturnedAt.AddDays(15) <= today // Đã 15 ngày
                     && !c.Invoices.Any(inv => inv.InvoiceType == InvoiceType.Refund))
            .ToListAsync();
        
        foreach (var contract in contractsForRefund)
        {
            try
            {
                var adminUserId = await GetSystemAdminUserId();
                await _invoiceService.CreateRefundInvoiceAsync(contract.ContractId, adminUserId);
                
                _logger.LogInformation("Auto-created Refund Invoice for contract {ContractCode}", 
                    contract.ContractCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Refund Invoice for contract {ContractId}", 
                    contract.ContractId);
            }
        }
    }
}
```

---

### 7. Xử lý hóa đơn quá hạn

**Scheduled Job** (chạy hàng ngày):

```csharp
public async Task CheckOverdueInvoicesAsync()
{
    var today = DateTime.Today;
    
    var overdueInvoices = await _context.Invoices
        .Where(i => i.Status == InvoiceStatus.Issued 
                 && i.DueDate.HasValue 
                 && i.DueDate.Value < today
                 && i.AmountDue > 0)
        .ToListAsync();
    
    foreach (var invoice in overdueInvoices)
    {
        invoice.Status = InvoiceStatus.Overdue;
        invoice.UpdatedAt = DateTime.Now;
        
        // Gửi email nhắc nhở khách hàng
        await SendOverdueNotificationAsync(invoice);
    }
    
    await _context.SaveChangesAsync();
}
```

---

## 📋 MIGRATION & DATA SEEDING

### Migration CreateInvoiceTables

```bash
dotnet ef migrations add AddInvoiceManagement
dotnet ef database update
```

### Seeding Data Mẫu

```csharp
// Trong UCarDataSeeder.cs
private async Task SeedInvoicesAsync()
{
    // Lấy các hợp đồng đã hoàn thành
    var completedContracts = await _context.RentalContracts
        .Where(c => c.Status == RentalContractStatus.Completed)
        .Take(10)
        .ToListAsync();
    
    foreach (var contract in completedContracts)
    {
        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            ContractId = contract.ContractId,
            CustomerId = contract.CustomerId,
            InvoiceType = InvoiceType.Rental,
            IssuedDate = contract.ActualEnd ?? DateTime.Now,
            DueDate = contract.ActualEnd?.AddDays(7),
            BaseRentalAmount = contract.RentalAmount,
            SurchargesTotal = contract.ExtraCharges,
            TotalAmount = contract.TotalAmountFinal,
            AmountPaid = contract.TotalAmountFinal, // Giả sử đã thanh toán
            AmountDue = 0,
            Status = InvoiceStatus.Paid,
            IssuedBy = contract.HandledBy,
            CreatedAt = DateTime.Now
        };
        
        _context.Invoices.Add(invoice);
    }
    
    await _context.SaveChangesAsync();
}
```

---

## 🎯 TÍCH HỢP VỚI HỆ THỐNG HIỆN TẠI

### 1. Thêm link trong menu

**Views/Shared/_Layout.cshtml**:
```html
@if (User.IsInRole("Admin") || User.IsInRole("BranchManager") || User.IsInRole("Staff"))
{
    <li>
        <a asp-controller="Invoice" asp-action="Index">
            <i class="material-icons">receipt_long</i>Quản lý hóa đơn
        </a>
    </li>
}
```

### 2. Thêm button "Tạo hóa đơn" trong Contract Details

**Views/Contract/Details.cshtml**:
```html
@if (Model.Status == RentalContractStatus.Completed)
{
    <a asp-controller="Invoice" asp-action="Create" asp-route-contractId="@Model.ContractId"
       class="btn green">
        <i class="material-icons left">receipt</i>Tạo hóa đơn
    </a>
}
```

### 3. Hiển thị hóa đơn trong Contract Details

```html
<!-- Trong Views/Contract/Details.cshtml -->
<div class="card">
    <div class="card-content">
        <span class="card-title">
            <i class="material-icons amber-text">receipt_long</i>
            Hóa đơn liên quan
        </span>
        
        @if (Model.Invoices != null && Model.Invoices.Any())
        {
            <table class="striped">
                <thead>
                    <tr>
                        <th>Mã HĐ</th>
                        <th>Loại</th>
                        <th>Ngày phát hành</th>
                        <th>Tổng tiền</th>
                        <th>Trạng thái</th>
                        <th>Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var invoice in Model.Invoices)
                    {
                        <tr>
                            <td>@invoice.InvoiceNumber</td>
                            <td>@invoice.InvoiceTypeText</td>
                            <td>@invoice.IssuedDate.ToString("dd/MM/yyyy")</td>
                            <td>@invoice.TotalAmount.ToString("N0")đ</td>
                            <td><span class="badge @invoice.StatusClass">@invoice.StatusText</span></td>
                            <td>
                                <a asp-controller="Invoice" asp-action="Details" asp-route-id="@invoice.InvoiceId"
                                   class="btn-small blue">Xem</a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        }
        else
        {
            <p class="grey-text">Chưa có hóa đơn nào</p>
        }
    </div>
</div>
```

---

## 📦 DEPENDENCIES CẦN THÊM

### 1. PDF Generation
```bash
dotnet add package iTextSharp.LGPLv2.Core
# hoặc
dotnet add package QuestPDF
```

### 2. Email Service (nếu chưa có)
```bash
dotnet add package MailKit
```

### 3. Excel Export (cho Statistics)
```bash
dotnet add package EPPlus
```

---

## 🚀 KẾ HOẠCH TRIỂN KHAI (5 PHASES)

### **PHASE 1: Database & Models** (1-2 ngày)
✅ **Mục tiêu:** Tạo cơ sở dữ liệu

**Tasks:**
1. Tạo `Invoice`, `InvoiceLineItem`, `InvoiceAdjustment` models
2. Tạo các Enums: `InvoiceType`, `InvoiceStatus`, `LineItemType`, `AdjustmentType`
3. Thêm `DbSet` vào `UCarDbContext`
4. Tạo migration `AddInvoiceManagement`
5. Chạy `dotnet ef database update`
6. Seed dữ liệu mẫu (10-20 invoices)

**Deliverable:** Database hoạt động với dữ liệu mẫu

---

### **PHASE 2: Core Service Layer** (2-3 ngày)
✅ **Mục tiêu:** Xây dựng business logic

**Tasks:**
1. Tạo `IInvoiceService` interface
2. Implement `InvoiceService`:
   - `CreateInvoiceFromContractAsync()` - Tạo HĐ tự động từ hợp đồng
   - `CalculateInvoiceAmountsAsync()` - Tính toán số tiền
   - `GetInvoicesAsync()` - Danh sách có filter + pagination
   - `GetInvoiceDetailsAsync()` - Chi tiết hóa đơn
   - `UpdateInvoiceStatusAsync()` - Cập nhật trạng thái
   - `RecordPaymentAsync()` - Ghi nhận thanh toán
3. Tạo các ViewModels:
   - `InvoiceListItemViewModel`
   - `InvoiceDetailsViewModel`
   - `InvoiceCreateViewModel`
   - `InvoicePrintViewModel`
4. Unit tests cho InvoiceService

**Deliverable:** Service layer hoạt động, có thể test bằng code

---

### **PHASE 3: Controller & Basic Views** (2-3 ngày)
✅ **Mục tiêu:** UI cơ bản cho CRUD

**Tasks:**
1. Tạo `InvoiceController`:
   - `Index()` - Danh sách
   - `Details()` - Chi tiết
   - `Create()` - Tạo mới
2. Tạo Views:
   - `Index.cshtml` - Table với filter, pagination
   - `Details.cshtml` - Hiển thị đầy đủ thông tin
   - `Create.cshtml` - Form tạo hóa đơn
3. Thêm menu "Quản lý hóa đơn" vào `_Layout.cshtml`
4. Thêm button "Tạo hóa đơn" trong `Contract/Details.cshtml`

**Deliverable:** Có thể xem, tạo, quản lý hóa đơn qua UI

---

### **PHASE 4: Print & Export** (2 ngày)
✅ **Mục tiêu:** In và xuất hóa đơn

**Tasks:**
1. Tạo `Print.cshtml` - Template in hóa đơn (CSS print-friendly)
2. Implement `ExportInvoiceToPdfAsync()` - Xuất PDF (iTextSharp/QuestPDF)
3. Implement `SendInvoiceByEmailAsync()` - Gửi email (MailKit)
4. Actions: `Print()`, `ExportPdf()`, `SendEmail()`
5. Test in trực tiếp trên trình duyệt
6. Test xuất PDF download
7. Test gửi email

**Deliverable:** Có thể in, xuất PDF, gửi email hóa đơn

---

### **PHASE 5: Integration & Advanced Features** (2-3 ngày)
✅ **Mục tiêu:** Tích hợp và tính năng nâng cao

**Tasks:**
1. **Auto-generate invoice khi complete contract:**
   - Hook vào `ContractService.CompleteContractAsync()`
   - Tự động tạo invoice
2. **Update invoice status khi thanh toán:**
   - Hook vào `PaymentService.ProcessPaymentAsync()`
   - Gọi `RecordPaymentAsync()`
3. **Invoice Adjustments:**
   - Form điều chỉnh hóa đơn
   - Lịch sử adjustments
4. **Cancel Invoice:**
   - Quyền Admin/Manager
   - Ghi nhận lý do hủy
5. **Statistics View:**
   - Biểu đồ doanh thu (Chart.js)
   - Export Excel
6. **Overdue checking:**
   - Scheduled job (Background Service)
   - Gửi email nhắc nhở
7. Testing toàn bộ luồng end-to-end

**Deliverable:** Hệ thống hoạt động hoàn chỉnh, tích hợp với module khác

---

## ✅ CHECKLIST HOÀN THÀNH

### Database
- [ ] Models: Invoice, InvoiceLineItem, InvoiceAdjustment
- [ ] Enums: InvoiceType, InvoiceStatus, LineItemType, AdjustmentType
- [ ] Migration đã chạy thành công
- [ ] Seed dữ liệu mẫu

### Service Layer
- [ ] IInvoiceService interface đầy đủ
- [ ] InvoiceService implement tất cả methods
- [ ] ViewModels đầy đủ
- [ ] Unit tests coverage >= 70%

### Controllers & Views
- [ ] InvoiceController với tất cả actions
- [ ] Index.cshtml - Danh sách + filter + pagination
- [ ] Details.cshtml - Chi tiết hóa đơn
- [ ] Create.cshtml - Form tạo mới
- [ ] Print.cshtml - Template in
- [ ] Statistics.cshtml - Báo cáo

### Integration
- [ ] Menu "Quản lý hóa đơn" trong _Layout
- [ ] Button "Tạo hóa đơn" trong Contract Details
- [ ] Hiển thị invoices trong Contract Details
- [ ] Auto-generate invoice khi complete contract
- [ ] Update invoice status khi thanh toán

### Features
- [ ] In hóa đơn (Print view)
- [ ] Xuất PDF
- [ ] Gửi email hóa đơn
- [ ] Điều chỉnh hóa đơn (Adjustments)
- [ ] Hủy hóa đơn (với quyền)
- [ ] Thống kê doanh thu
- [ ] Kiểm tra hóa đơn quá hạn

### Testing
- [ ] Unit tests cho InvoiceService
- [ ] Integration tests cho InvoiceController
- [ ] End-to-end test luồng: Create Contract → Complete → Generate Invoice → Payment → Update Status
- [ ] Test in hóa đơn trên nhiều trình duyệt
- [ ] Test xuất PDF
- [ ] Test gửi email

---

## 📌 LƯU Ý QUAN TRỌNG

### 1. Phân quyền
- **Admin, BranchManager:** Toàn quyền (tạo, sửa, xóa, hủy invoice)
- **Staff:** Chỉ xem và tạo invoice
- **Customer:** Không truy cập (có thể tạo portal riêng sau)

### 2. Bảo mật
- Validate số tiền không được âm
- Chỉ cho phép hủy invoice nếu chưa thanh toán
- Ghi log tất cả thay đổi trên invoice
- Kiểm tra quyền trước khi thực hiện thao tác nhạy cảm

### 3. Performance
- Index invoice table (InvoiceNumber, ContractId, CustomerId, Status, IssuedDate)
- Pagination bắt buộc (không load toàn bộ)
- Lazy loading cho LineItems và Adjustments
- Cache template in hóa đơn

### 4. UX
- Highlight hóa đơn quá hạn (màu đỏ)
- Filter sticky (giữ state sau khi navigate back)
- Loading spinner khi xuất PDF
- Toast notification khi gửi email thành công
- Confirm dialog trước khi hủy invoice

---

## 🎓 TÀI LIỆU THAM KHẢO

### Invoicing Best Practices
- [Invoice Design Principles](https://www.invoiceberry.com/invoice-design-tips)
- [PDF Generation with iTextSharp](https://github.com/VahidN/iTextSharp.LGPLv2.Core)
- [QuestPDF Documentation](https://www.questpdf.com/)

### Email Templates
- [Professional Invoice Email Templates](https://www.mailjet.com/blog/invoice-email/)

### Legal Requirements (Vietnam)
- Quy định về hóa đơn điện tử
- Thuế VAT 10% (nếu áp dụng)
- Thông tin bắt buộc trên hóa đơn

---

## 📞 SUPPORT

- **Technical Issues:** Liên hệ team lead
- **Business Logic:** Review với BA/PM
- **UI/UX:** Consult với designer

---

**Tạo bởi:** Tài  
**Ngày:** 04/02/2026  
**Version:** 1.0  
**Status:** Ready for Implementation
