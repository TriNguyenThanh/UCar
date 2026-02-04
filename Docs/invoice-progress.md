# Invoice Management Module - Progress Tracker
**Module 7 - Quản lý hóa đơn**
*Cập nhật lần cuối: 03/02/2025*

---

## 🎯 TÓM TẮT DỰ ÁN

**Mục tiêu:** Xây dựng hệ thống quản lý hóa đơn theo 4 giai đoạn thanh toán
**Trạng thái tổng thể:** ✅ Phase 1-3 HOÀN THÀNH (67%) | 🔄 Phase 4-5 ĐANG CHỜ (33%)

### Kiến trúc 4 giai đoạn thanh toán:
1. **Deposit Invoice** - Khi hợp đồng Active → Tính cọc trách nhiệm + cọc thuê (từ DepositPolicy)
2. **Rental Invoice** - Khi bàn giao xe → Hiển thị breakdown chi tiết từ ContractPriceBreakdown, trừ cọc thuê
3. **Surcharge/Penalty Invoice** - Khi trả xe + có phí phát sinh → Map từ ContractCharges
4. **Refund Invoice** - Sau RefundProcessingDays → Tự động hoàn cọc (cọc - phí chưa trả)

---

## ✅ PHASE 1: DATABASE LAYER (HOÀN THÀNH 100%)

### 1.1 Enums (4 files - 34 enum values) ✅
- [x] **InvoiceType.cs** (5 values)
  - Deposit, Rental, Surcharge, Penalty, Refund
  - Mỗi type có XML comment giải thích khi nào dùng
  
- [x] **InvoiceStatus.cs** (7 values)
  - Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled, Refunded
  - Control workflow và UI display
  
- [x] **LineItemType.cs** (18 values)
  - Organized by invoice type:
    * Deposit: ResponsibilityDeposit, RentalDeposit
    * Rental: BaseRental, PeakRental, MonthlyRental
    * Surcharge: OvertimeSurcharge, ExtraKmSurcharge, CleaningSurcharge, FuelSurcharge
    * Penalty: ContractViolation, DamagePenalty
    * Refund: RefundResponsibility, RefundRental
    * Other: Discount, Tax, Other
  - Mỗi type link về Module 3 source trong comments
  
- [x] **AdjustmentType.cs** (4 values)
  - Correction, Discount, TaxAdjustment, Other
  - Cho audit trail

### 1.2 Models (3 files) ✅
- [x] **Invoice.cs** (27 properties)
  - Basic: InvoiceId, InvoiceNumber (unique, indexed), InvoiceType, Status
  - Relationships: ContractId, CustomerId, IssuedBy (StaffId)
  - Amounts: BaseRentalAmount, SurchargesTotal, PenaltiesTotal, DepositPaid, DiscountAmount, TaxAmount
  - Final: TotalAmount, AmountPaid, AmountDue
  - Dates: IssuedDate, DueDate, CreatedAt, UpdatedAt
  - Notes: Notes (customer-facing), InternalNotes (staff-only)
  - Navigation: Contract, Customer, IssuedByStaff, LineItems, Adjustments
  
- [x] **InvoiceLineItem.cs** (10 properties)
  - LineItemId, InvoiceId, ItemType, Description
  - Breakdown: Quantity, Unit, UnitPrice, Amount
  - Reference: ReferenceId (link to ContractCharge), Notes
  - Navigation: Invoice
  
- [x] **InvoiceAdjustment.cs** (9 properties)
  - AdjustmentId, InvoiceId, AdjustmentType
  - Details: Amount, Reason, Notes
  - Audit: AdjustedBy (StaffId), AdjustedAt
  - Navigation: Invoice, AdjustedByStaff

### 1.3 DbContext Configuration ✅
- [x] **Added 3 DbSets** to UCarDbContext
  - `public DbSet<Invoice> Invoices { get; set; }`
  - `public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }`
  - `public DbSet<InvoiceAdjustment> InvoiceAdjustments { get; set; }`
  
- [x] **Enum String Conversions** (4 enums)
  - Invoice.InvoiceType → string
  - Invoice.Status → string
  - InvoiceLineItem.ItemType → string
  - InvoiceAdjustment.AdjustmentType → string
  - Reason: Readability trong DB và không breaking change khi thêm enum
  
- [x] **Foreign Key Configurations**
  - Invoice → RentalContract (OnDelete: Restrict) ← Không xóa contract nếu có invoice
  - Invoice → Customer (OnDelete: Restrict) ← Không xóa customer nếu có invoice
  - Invoice → StaffProfile (IssuedBy, OnDelete: Restrict) ← Keep issuer history
  - InvoiceLineItem → Invoice (OnDelete: Cascade) ← Xóa invoice thì xóa line items
  - InvoiceAdjustment → Invoice (OnDelete: Cascade) ← Xóa invoice thì xóa adjustments
  - InvoiceAdjustment → StaffProfile (AdjustedBy, OnDelete: Restrict) ← Keep adjuster history
  
- [x] **Unique Constraints**
  - Invoice.InvoiceNumber (unique index)

### 1.4 Migration ✅
- [x] **20260203194036_AddInvoiceManagement.cs**
  - Created 3 tables: Invoices, InvoiceLineItems, InvoiceAdjustments
  - Indexes: InvoiceNumber unique index
  - Successfully applied to database
  - Fixed issue: Changed all Invoice FK to DeleteBehavior.Restrict để tránh cascade path conflicts

### 1.5 Integration với Module 3 ✅
- [x] **DepositPolicy** usage confirmed
  - ResponsibilityDepositAmount: Fixed amount per vehicle type
  - RentalDepositCalculationType: Percentage hoặc FixedAmount
  - RentalDepositValue: Value to calculate rental deposit
  - RentalDepositMinimum/Maximum: Bounds checking
  - RefundProcessingDays: 15-30 days wait before refund
  
- [x] **SurchargePolicy** usage confirmed
  - VehicleTypeId lookup
  - OvertimeFee, ExtraKilometer, CleaningFee, FuelShortage
  - CalculationType + Value for each
  
- [x] **Snapshot Pricing** from RentalContract
  - SnapshotBaseDailyPrice, SnapshotPeakMultiplier, SnapshotOvertimeHourlyPrice
  - ContractPriceBreakdown: PriceType, Days, UnitPrice, TotalAmount

---

## ✅ PHASE 2: BUSINESS LOGIC LAYER (HOÀN THÀNH 100%)

### 2.1 IInvoiceService Interface ✅
- [x] **15 methods** organized into 5 categories:
  1. **Create Invoices** (4 methods)
     - CreateDepositInvoiceAsync
     - CreateRentalInvoiceAsync
     - CreateSurchargePenaltyInvoiceAsync
     - CreateRefundInvoiceAsync
  2. **Payment Management** (1 method)
     - RecordPaymentAsync
  3. **Retrieval** (3 methods)
     - GetInvoicesAsync (with filter + pagination)
     - GetInvoiceDetailsAsync
     - GetInvoicesByContractIdAsync
  4. **Status & Validation** (2 methods)
     - CanCreateDepositInvoiceAsync
     - GetInvoiceNumberAsync
  5. **Background Services** (2 methods)
     - ProcessRefundInvoicesAsync (auto-create refund invoices)
     - CheckOverdueInvoicesAsync (update status to Overdue)

### 2.2 InvoiceService Implementation ✅ (730+ lines)
#### Create Methods:
- [x] **CreateDepositInvoiceAsync** (lines 23-134)
  - Load DepositPolicy cho vehicle type
  - Calculate ResponsibilityDeposit (fixed)
  - Calculate RentalDeposit (with min/max constraints)
  - Create 2 line items (ResponsibilityDeposit, RentalDeposit)
  - Generate invoice number format: DEP-YYMM-0001
  - Check for existing deposit invoice (idempotent)
  - Status: Issued, DueDate: trong 7 ngày
  
- [x] **CreateRentalInvoiceAsync** (lines 136-269)
  - Read snapshot pricing from RentalContract
  - Create separate line items cho:
    * Normal days × BaseDailyPrice
    * Peak days × PeakPrice
    * Monthly rate (if applicable)
  - Subtract rental deposit as negative line item
  - Calculate TotalAmount, AmountDue
  - If amountDue <= 0 (deposit đủ trả), mark as Paid
  - Generate invoice number: RNT-YYMM-0001
  - Status: Issued or Paid, DueDate: trong 3 ngày
  
- [x] **CreateSurchargePenaltyInvoiceAsync** (lines 271-369)
  - Load unpaid ContractCharges
  - Determine invoice type (Surcharge vs Penalty) based on majority
  - Create line items với ReferenceId linking to charges
  - Map ChargeType → LineItemType:
    * OvertimeFee → OvertimeSurcharge
    * CleaningFee → CleaningSurcharge
    * FuelShortage → FuelSurcharge
    * DamageFee → DamagePenalty
    * AccessoryLoss → ContractViolation
  - Only create if charges exist
  - Generate invoice number: SUR-YYMM-0001 or PEN-YYMM-0001
  - Status: Issued, DueDate: trong 7 ngày
  
- [x] **CreateRefundInvoiceAsync** (lines 371-462)
  - Calculate total deposits paid (from Deposit invoice)
  - Sum unpaid charges (from Surcharge/Penalty invoices)
  - Create negative amount (refund to customer)
  - 3 line items:
    * RefundResponsibility (refund cọc trách nhiệm)
    * RefundRental (refund cọc thuê)
    * Minus unpaid charges (negative amount)
  - Generate invoice number: REF-YYMM-0001
  - Status: Issued (phải thực hiện refund)

#### Payment Management:
- [x] **RecordPaymentAsync** (lines 610-640)
  - Update AmountPaid += payment
  - Recalculate AmountDue = TotalAmount - AmountPaid
  - Transition status:
    * Issued → PartiallyPaid (if AmountDue > 0)
    * Issued/PartiallyPaid → Paid (if AmountDue <= 0)
    * Refund type → Refunded (when refund processed)
  - Append payment log to InternalNotes
  - Update UpdatedAt timestamp

#### Background Services:
- [x] **ProcessRefundInvoicesAsync** (lines 688-714)
  - Find contracts: Status = Completed AND DepositRefundDueDate <= today AND no Refund Invoice
  - Auto-create Refund Invoice for each
  - Designed to run daily via background job
  
- [x] **CheckOverdueInvoicesAsync** (lines 716-730)
  - Find invoices: Status = Issued/PartiallyPaid AND DueDate < now AND AmountDue > 0
  - Update Status to Overdue
  - Designed to run daily via background job

#### Helper Methods:
- [x] **GenerateInvoiceNumberAsync** (lines 495-513)
  - Format: PREFIX-YYMM-0001
  - Prefixes: DEP (Deposit), RNT (Rental), SUR (Surcharge), PEN (Penalty), REF (Refund)
  - Find last number for current month, increment
  - Thread-safe with transaction
  
- [x] **DetermineLineItemTypeFromCharge** (lines 642-662)
  - Map ChargeType enum → LineItemType enum
  - Used in CreateSurchargePenaltyInvoiceAsync

### 2.3 Service Registration ✅
- [x] **Program.cs** updated
  - `builder.Services.AddScoped<IInvoiceService, InvoiceService>();`
  - Registered before app.Build()

### 2.4 Bug Fixes During Implementation ✅
1. **Issue:** Vehicle.VehicleModel not found
   - **Fix:** Changed to Vehicle.Model (navigation property)
   - **Locations:** InvoiceService (4 places), UCarDbContext includes

2. **Issue:** ChargeType enum values mismatch
   - **Fix:** Updated DetermineLineItemTypeFromCharge with correct values:
     * OvertimeFee (not Overtime)
     * LateFee → ExtraKmSurcharge (temporary mapping)
     * CleaningFee, FuelShortage, DamageFee, AccessoryLoss

3. **Issue:** RentalContractStatus.Returned not found
   - **Fix:** Changed to RentalContractStatus.Completed
   - **Location:** ProcessRefundInvoicesAsync

4. **Issue:** Customer.Phone/Email not found
   - **Fix:** Access via Customer.UserAccount.Phone/Email
   - **Added:** .ThenInclude(c => c.UserAccount) in service method

5. **Issue:** DepositPolicy min/max HasValue error
   - **Fix:** Changed from nullable check to > 0 comparison
   - **Reason:** Fields are decimal, not decimal?

---

## ✅ PHASE 3: UI LAYER (HOÀN THÀNH 100%)

### 3.1 ViewModels ✅ (6 files - 300+ lines)
- [x] **InvoiceListViewModel**
  - For Index view with filter
  - Properties: InvoiceId, InvoiceNumber, InvoiceType, ContractCode, CustomerName, VehiclePlateNo, TotalAmount, PreviouslyPaid, AmountDue, Status, IssuedDate, DueDate
  - Display helpers:
    * InvoiceTypeDisplay: Map enum → Vietnamese text
    * StatusDisplay: Map status → Vietnamese text
    * StatusBadgeClass: Map status → CSS class (badge bg-success, etc.)
    * IsOverdue: Boolean check (Status == Overdue)
    * CanRecordPayment: Boolean check (Issued/PartiallyPaid/Overdue)
  
- [x] **InvoiceDetailsViewModel**
  - Full invoice details with all breakdowns
  - Properties: All invoice fields + Contract info + Customer info + LineItems + Adjustments
  - Display helpers: Same as ListViewModel + CanCancel
  
- [x] **InvoiceLineItemViewModel**
  - Individual line item details
  - Properties: LineItemId, ItemType, Description, Quantity, Unit, UnitPrice, Amount, Notes
  - Display helper:
    * ItemTypeDisplay: Map 18 types → Vietnamese text
      - ResponsibilityDeposit → "Cọc trách nhiệm"
      - RentalDeposit → "Cọc thuê xe"
      - BaseRental → "Thuê ngày thường"
      - OvertimeSurcharge → "Phụ phí vượt giờ"
      - etc.
  
- [x] **InvoiceAdjustmentViewModel**
  - Adjustment history
  - Properties: AdjustmentId, AdjustmentType, Amount, Reason, AdjustedByName, AdjustedAt, Notes
  - Display helper:
    * AdjustmentTypeDisplay: Correction → "Sửa lỗi", Discount → "Giảm giá", etc.
  
- [x] **InvoicePrintViewModel**
  - Print template
  - Properties: Invoice info + Company info (hardcoded) + Customer info + Contract info + LineItems + Amounts + RentalDays
  - Company defaults:
    * CompanyName: "UCar - Hệ thống cho thuê xe"
    * CompanyAddress: "123 Đường ABC, Quận 1, TP.HCM"
    * CompanyPhone: "028-1234-5678"
    * CompanyEmail: "contact@ucar.vn"
    * CompanyTaxCode: "0123456789"
  - Calculated fields:
    * RentalDays: (RentalEnd - RentalStart).Days
    * Subtotal: Sum of line items
  
- [x] **RecordPaymentViewModel**
  - Payment form
  - Properties:
    * InvoiceId, InvoiceNumber (display)
    * AmountPaid (editable, required, Range 0.01-max)
    * Notes (optional, MaxLength 500)
    * AmountDue, TotalAmount, PreviouslyPaid (display)
    * DueDate, IsOverdue (display)
  - Validation attributes:
    * Required on AmountPaid
    * Range(0.01, double.MaxValue)
    * MaxLength(500) on Notes

### 3.2 InvoiceController ✅ (6 actions - 400+ lines)
#### Authorization:
- [x] Controller-level: `[Authorize(Roles = "Admin,BranchManager,Staff")]`

#### Actions:
- [x] **Index (GET)** - List invoices with filter
  - Parameters: status, type, fromDate, toDate, searchTerm, page
  - Filter logic: Call service.GetInvoicesAsync with all filters
  - Pagination: 15 items per page
  - ViewBag: Store all filter values + pagination info (CurrentPage, TotalPages, TotalCount)
  - Mapping: Invoice → InvoiceListViewModel with PreviouslyPaid = AmountPaid
  - Error handling: Log error, show empty list with message
  
- [x] **Details (GET)** - View full invoice
  - Parameter: id (InvoiceId)
  - Load with includes: Contract.Vehicle.Model, Customer.UserAccount, LineItems, Adjustments.AdjustedByStaff
  - Mapping: Invoice → InvoiceDetailsViewModel
    * VehicleModelName = Vehicle.Model.ModelName
    * CustomerPhone/Email = UserAccount.Phone/Email
    * LineItems collection
    * Adjustments collection
  - Error handling: Redirect to Index if not found
  
- [x] **Print (GET)** - Print-friendly view
  - Parameter: id (InvoiceId)
  - No layout, clean print CSS
  - Calculate RentalDays: (PlannedEnd - PlannedStart).Days
  - Mapping: Invoice → InvoicePrintViewModel
    * InvoiceType enum for conditional rendering
    * InvoiceTypeText: Vietnamese uppercase text
    * Status + StatusDisplay
    * VehicleModelName = Vehicle.Model.ModelName
    * CustomerEmail = UserAccount.Email
    * BaseRentalAmount, SurchargesTotal, PenaltiesTotal, DepositPaid
    * Subtotal = Base + Surcharges + Penalties
  - Auto-print on load: JavaScript window.print()
  
- [x] **RecordPayment (GET)** - Show payment form
  - Parameter: id (InvoiceId)
  - Validation: CanRecordPayment check (Status must be Issued/PartiallyPaid/Overdue)
  - Pre-fill: AmountPaid = AmountDue (full payment by default)
  - Mapping: Invoice → RecordPaymentViewModel
    * TotalAmount, PreviouslyPaid, AmountDue
    * DueDate, IsOverdue
  - Error handling: Redirect to Details if can't record payment
  
- [x] **RecordPayment (POST)** - Process payment
  - Parameter: RecordPaymentViewModel
  - ModelState validation
  - Extract StaffId from User.Claims
  - Call service.RecordPaymentAsync(InvoiceId, AmountPaid, StaffId, Notes)
  - Success message: "Đã ghi nhận thanh toán {amount} VNĐ thành công"
  - Redirect to Details with success message
  - Error handling: Return to form with error message
  
- [x] **ByContract (GET)** - View all invoices for one contract
  - Parameter: contractId
  - Load all invoices ordered by IssuedDate
  - ViewBag: ContractCode, VehiclePlateNo, VehicleModelName, CustomerName (for display)
  - Mapping: List<Invoice> → List<InvoiceListViewModel>
  - Display: Timeline view + Detailed table view + Total row
  - Error handling: Show empty state if no invoices

#### Error Handling Pattern:
All actions wrapped in try-catch:
```csharp
try
{
    // Action logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error message");
    TempData["ErrorMessage"] = "User-facing message";
    return RedirectToAction(...) or View(model);
}
```

#### Helper Methods:
- [x] **CanRecordPayment** (private)
  - Check if Status == Issued || PartiallyPaid || Overdue
  - Used in RecordPayment GET action

### 3.3 Views ✅ (5 files)
#### UI Framework Stack:
- **Materialize CSS 1.0.0** (from CDN)
- **Material Icons** (Google hosted)
- **iziToast 1.4.0** (for notifications)
- **Be Vietnam Pro** font (Google Fonts)
- **ucar-theme.css** (custom theme)

#### Common Patterns:
- Structure: Container → Card → Card-content → Form/Table
- Forms: input-field with prefix icons, select dropdowns
- Tables: striped + highlight + responsive-table
- Buttons: waves-effect waves-light with color classes
- Colors: Blue for primary, Red for overdue/debt, Green for paid/refund
- Icons: Material Icons only (not Font Awesome)
- Currency: :N0 format (Vietnamese: 1.000.000)
- Dates: dd/MM/yyyy format

- [x] **Index.cshtml** - Invoice list with filter ✅
  - Model: `List<InvoiceListViewModel>`
  - Page header: Icon + Title + Total count
  - Filter card (card structure):
    * Search input (searchTerm) with Material Icons prefix
    * Type dropdown (5 options): Đặt cọc, Tiền thuê, Phụ phí, Phạt, Hoàn cọc
    * Status dropdown (4 options): Đã phát hành, Trả một phần, Đã thanh toán, Quá hạn
    * Date range: fromDate, toDate (input type="date")
    * Submit button: Full width với icon search
  - Results table (striped highlight responsive-table):
    * Columns: Mã HĐ (with IssuedDate), Loại (badge), Hợp đồng (link), Khách, Xe (chip), Tổng tiền, Còn phải trả, Trạng thái (badge), Hạn TT, Actions
    * Row highlight: Red lighten-5 if IsOverdue
    * Invoice type badges: Blue (Deposit), Green (Rental), Orange (Surcharge), Red (Penalty), Teal (Refund)
    * Contract link: asp-controller="Contract" asp-action="Details"
    * Actions: Details button (blue), RecordPayment button (green, if CanRecordPayment)
  - Pagination: ul.pagination với active class on current page
  - Empty state: Icon + Message "Không tìm thấy hóa đơn nào"
  - Scripts section:
    * Initialize Materialize select dropdowns: M.FormSelect.init()
    * Initialize tooltips: M.Tooltip.init()
    * Update labels for date inputs if pre-filled
  
- [x] **Details.cshtml** - Full invoice breakdown ✅
  - Model: `InvoiceDetailsViewModel`
  - Page header: Icon + InvoiceNumber + InvoiceTypeDisplay + Actions (Print, RecordPayment)
  - Status badge: Large badge with color + Overdue warning if applicable
  - Layout: 2 columns (Customer + Contract | Invoice Summary)
  - Left column:
    * Customer card: Name, Phone, Email (with "Chưa cập nhật" if null)
    * Contract card: ContractCode (link), Vehicle (chip + model name)
  - Right column:
    * Invoice summary card (blue lighten-5):
      - BaseRentalAmount (if > 0)
      - SurchargesTotal (orange text if > 0)
      - PenaltiesTotal (red text if > 0)
      - DiscountAmount (green text if > 0)
      - DepositPaid (blue text if > 0)
      - TotalAmount (large, bold, blue/green text based on sign)
      - AmountPaid (green text)
      - AmountDue (large, bold, red/green text based on sign)
      - DueDate box (red if IsOverdue)
  - Line items card:
    * Table: Mục, Mô tả (with Notes), Số lượng, Đơn vị, Đơn giá, Thành tiền
    * ItemTypeDisplay for each item
    * Green text for negative amounts (refunds)
  - Adjustments card (if any):
    * Table: Loại điều chỉnh, Số tiền, Lý do, Người điều chỉnh, Thời gian
    * Red/Green text based on amount sign
  - Notes card (if Notes not empty):
    * white-space: pre-wrap to preserve formatting
  - Internal notes card (yellow lighten-4, if InternalNotes not empty):
    * Lock icon + "Chỉ nhân viên" title
    * white-space: pre-wrap
  - Action buttons: Back to Index, View all invoices for contract
  
- [x] **Print.cshtml** - Print template ✅
  - Model: `InvoicePrintViewModel`
  - Layout: null (no navigation, no footer)
  - DOCTYPE html, lang="vi"
  - Custom CSS (embedded):
    * Body: font Arial, padding 20mm
    * Header: Company logo area + Invoice title
    * Info sections: 2-column grid with background
    * Table: Clean borders, no hover effects
    * Summary: Right-aligned with bold totals
    * Signature: 2 boxes for Issuer and Customer
    * Footer: Thank you message + Print timestamp
    * @media print: Remove padding, set @page margin 15mm
  - Header section:
    * Company info: CompanyName, Address, Phone, Email, TaxCode
  - Invoice title:
    * "HÓA ĐƠN {InvoiceTypeText.ToUpper()}"
    * InvoiceNumber + Type badge (conditional rendering based on InvoiceType enum)
  - Info blocks (2 columns):
    * Customer info: Name, Phone, Email
    * Invoice info: IssuedDate, DueDate, StatusDisplay
    * Contract info: ContractCode, VehiclePlateNo + VehicleModelName, RentalDays (if present)
  - Line items table:
    * For loop with index: STT, Mục, Số lượng, Đơn vị, Đơn giá, Thành tiền
    * ItemTypeDisplay + Description
    * Green text for negative amounts
  - Summary section (right-aligned):
    * BaseRentalAmount (if > 0, orange)
    * SurchargesTotal (if > 0, orange)
    * PenaltiesTotal (if > 0, red)
    * DiscountAmount (if > 0, green)
    * DepositPaid (if > 0, blue)
    * TotalAmount (large, bold, blue/green based on sign)
    * AmountPaid (green)
    * AmountDue (large, bold, red/green based on sign)
  - Notes section (yellow background):
    * "Ghi chú:" header + Notes text
  - Signature section:
    * 2 columns: "Người lập phiếu" | "Khách hàng"
    * Space for signatures (80px)
  - Footer:
    * Thank you message
    * "Hóa đơn tự động" note
    * Print timestamp: DateTime.Now
  - Script: window.onload → window.print() (auto-print)
  
- [x] **RecordPayment.cshtml** - Payment form ✅
  - Model: `RecordPaymentViewModel`
  - Layout: 2 columns (Form | Summary + Instructions)
  - Left column (Form card):
    * InvoiceNumber (readonly with receipt icon)
    * AmountDue display (readonly, red icon, formatted: :N0 ₫)
    * AmountPaid input (editable, green icon, type="number", step="1000", min="0", max=AmountDue)
    * Quick amount buttons:
      - "Toàn bộ" → setAmount(AmountDue)
      - "50%" → setAmount(AmountDue / 2)
      - "Xóa" → setAmount(0)
    * PaymentDate display (readonly, auto-filled: DateTime.Now dd/MM/yyyy HH:mm)
    * Notes textarea (optional, maxlength="500" with character counter)
    * ValidationSummary: asp-validation-summary="ModelOnly"
    * Action buttons: Submit (green, "Xác nhận thanh toán"), Cancel (grey, back to Details)
  - Right column:
    * Invoice summary card (blue lighten-5):
      - TotalAmount
      - PreviouslyPaid (green)
      - AmountDue (large, bold, red)
      - DueDate (with IsOverdue check, red if overdue)
    * Payment instructions card (yellow lighten-5):
      - 4 bullet points explaining process
    * Quick actions card:
      - "Xem chi tiết hóa đơn" button
      - "In hóa đơn" button (target="_blank")
  - Form validation:
    * asp-validation-for on AmountPaid and Notes
    * Client-side: Check AmountPaid > 0 and <= AmountDue
    * Client-side: Confirm dialog before submit
    * Materialize toast for errors
  - Scripts section:
    * Initialize character counter: M.CharacterCounter.init()
    * Update text fields: M.updateTextFields()
    * Form submit handler: Validate amount, show confirm dialog
    * setAmount(amount) function: Set input value, update labels
  - Uses _ValidationScriptsPartial for jQuery validation
  
- [x] **ByContract.cshtml** - Timeline view for one contract ✅
  - Model: `List<InvoiceListViewModel>`
  - Page header: Icon + "Hóa đơn theo hợp đồng" + ContractCode + VehiclePlateNo chip
  - Contract summary card (blue lighten-5):
    * 3 columns: Customer name, Vehicle info, Total count
  - Timeline view (card):
    * Title: "Luồng thanh toán" with timeline icon
    * 4 columns (responsive: col s12 m6 l3):
      - Each invoice: Card with top border (color based on type)
      - Type badge with icon (lock/attach_money/add/warning/replay)
      - InvoiceNumber (large, bold)
      - IssuedDate with event icon
      - Amounts box (grey background):
        * Tổng tiền (with color based on sign)
        * Còn phải trả (with color based on sign)
      - Status badge
      - Overdue badge (if applicable)
      - Actions: Details button (full width), RecordPayment button (if CanRecordPayment)
  - Detailed table view (card):
    * Header: "Bảng chi tiết" with table_chart icon
    * Table: Same columns as Index table
    * Red lighten-5 row highlight if IsOverdue
    * Footer: Total row with sum of TotalAmount, PreviouslyPaid, AmountDue
  - Empty state: Icon + "Chưa có hóa đơn nào"
  - Back buttons: Back to Index, View contract details (if ContractId provided)
  - @functions section:
    * GetColorForType(InvoiceType): Return hex color for border
      - Deposit: #2196F3 (blue)
      - Rental: #4CAF50 (green)
      - Surcharge: #FF9800 (orange)
      - Penalty: #F44336 (red)
      - Refund: #009688 (teal)
  - Scripts section:
    * Initialize tooltips: M.Tooltip.init()

### 3.4 Build Status ✅
- [x] **Build succeeded**
  - 0 errors
  - 6 warnings (existing warnings from other modules)
  - All ViewModels properties mapped correctly
  - All Controller actions compile without errors
  - All Views render without compilation errors

### 3.5 Bug Fixes During UI Development ✅
1. **Issue:** VehicleModel property name mismatch
   - **Fix:** Renamed to VehicleModelName in all ViewModels
   - **Locations:** InvoiceDetailsViewModel, InvoicePrintViewModel
   - **Controller updates:** Details, Print actions

2. **Issue:** Missing PreviouslyPaid in InvoiceListViewModel
   - **Fix:** Added property with [Display(Name = "Đã thanh toán")]
   - **Controller update:** Index action mapping
   - **View usage:** ByContract table

3. **Issue:** Missing properties in InvoicePrintViewModel
   - **Fix:** Added InvoiceType, Status, CustomerEmail, BaseRentalAmount, SurchargesTotal, PenaltiesTotal, DepositPaid
   - **Added StatusDisplay helper**
   - **Controller update:** Print action mapping with all required fields

4. **Issue:** RecordPaymentViewModel property name mismatch
   - **Fix:** Renamed Amount → AmountPaid
   - **Added:** TotalAmount, PreviouslyPaid, DueDate, IsOverdue properties
   - **Controller updates:** RecordPayment GET (pre-fill), RecordPayment POST (use AmountPaid)
   - **View updates:** RecordPayment.cshtml form binding

5. **Issue:** RentalDays type mismatch
   - **Fix:** Changed from `int` to `int?` (nullable) in InvoicePrintViewModel
   - **View update:** Print.cshtml uses HasValue check

---

## 🔄 PHASE 4: INTEGRATION (ĐANG CHỜ)

### 4.1 Integration với ContractController ❌
- [ ] **Add auto-create Deposit Invoice**
  - Location: Khi contract status changes to Active
  - Call: `await _invoiceService.CreateDepositInvoiceAsync(contractId, staffId)`
  - Error handling: Log error, don't block contract activation
  - UI feedback: TempData success message with invoice number

### 4.2 Integration với HandoverController ❌
- [ ] **Add auto-create Rental Invoice**
  - Location: Khi HandoverRecord created (xe đã bàn giao)
  - Call: `await _invoiceService.CreateRentalInvoiceAsync(contractId, staffId)`
  - Error handling: Log error, don't block handover process
  - UI feedback: TempData with link to invoice

### 4.3 Integration với ReturnController ❌
- [ ] **Add auto-create Surcharge/Penalty Invoice**
  - Location: Khi ReturnRecord created AND ContractCharges exist
  - Check: `var unpaidCharges = await _context.ContractCharges.Where(c => c.ContractId == contractId && !c.IsPaid).ToListAsync()`
  - Call: `if (unpaidCharges.Any()) await _invoiceService.CreateSurchargePenaltyInvoiceAsync(contractId, staffId)`
  - Error handling: Log error, don't block return process
  - UI feedback: TempData with link to invoice

### 4.4 Schedule Background Services ❌
- [ ] **Setup Hangfire or similar**
  - Install package: `dotnet add package Hangfire.AspNetCore`
  - Configure in Program.cs:
    ```csharp
    builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
    builder.Services.AddHangfireServer();
    ```
  - Schedule jobs:
    ```csharp
    RecurringJob.AddOrUpdate<IInvoiceService>("process-refunds", 
        x => x.ProcessRefundInvoicesAsync(), 
        Cron.Daily(2)); // 2 AM every day
    
    RecurringJob.AddOrUpdate<IInvoiceService>("check-overdue", 
        x => x.CheckOverdueInvoicesAsync(), 
        Cron.Hourly); // Every hour
    ```
  - Add dashboard: `app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireAuthFilter() } })`

---

## 🧪 PHASE 5: TESTING (ĐANG CHỜ)

### 5.1 Full Workflow Test ❌
- [ ] **Test Case 1: Happy Path**
  1. Create contract → Status Active
     - Verify: Deposit Invoice created
     - Verify: Invoice number format DEP-YYMM-0001
     - Verify: 2 line items (ResponsibilityDeposit, RentalDeposit)
     - Verify: AmountDue = ResponsibilityDeposit + RentalDeposit
  2. Record deposit payment
     - Verify: AmountPaid updated
     - Verify: Status → Paid
     - Verify: InternalNotes updated with payment log
  3. Create handover record
     - Verify: Rental Invoice created
     - Verify: Invoice number format RNT-YYMM-0001
     - Verify: Line items show breakdown (normal days, peak days)
     - Verify: Rental deposit subtracted (negative line item)
     - Verify: AmountDue = Rental - RentalDeposit
  4. Record rental payment
     - Verify: Status → Paid
  5. Create return record with charges
     - Verify: Surcharge Invoice created (or Penalty based on charges)
     - Verify: Line items match ContractCharges
     - Verify: ReferenceId links to charges
  6. Record surcharge payment
     - Verify: Status → Paid
  7. Wait for refund due date (or manually set DepositRefundDueDate to past)
     - Run: ProcessRefundInvoicesAsync manually
     - Verify: Refund Invoice created
     - Verify: Invoice number format REF-YYMM-0001
     - Verify: Amount = (ResponsibilityDeposit + RentalDeposit) - (unpaid charges if any)
     - Verify: 3 line items (RefundResponsibility, RefundRental, minus unpaid)
  8. Record refund completion
     - Verify: Status → Refunded

- [ ] **Test Case 2: Partial Payments**
  1. Create deposit invoice
  2. Pay 50% of deposit
     - Verify: Status → PartiallyPaid
     - Verify: AmountDue = TotalAmount - AmountPaid
  3. Pay remaining 50%
     - Verify: Status → Paid
     - Verify: AmountDue = 0

- [ ] **Test Case 3: Overdue Invoices**
  1. Create invoice with DueDate in past
  2. Run: CheckOverdueInvoicesAsync
     - Verify: Status → Overdue
  3. Pay overdue invoice
     - Verify: CanRecordPayment = true (even when Overdue)
     - Verify: Status → Paid after payment

- [ ] **Test Case 4: Refund with Unpaid Charges**
  1. Complete contract with deposit paid
  2. Create return with charges (not paid)
  3. Wait for refund due date
  4. Run: ProcessRefundInvoicesAsync
     - Verify: Refund amount = deposits - unpaid charges
     - Verify: If unpaid charges > deposits, refund amount = 0 (no refund)

### 5.2 Edge Cases ❌
- [ ] **Test: Create duplicate invoice**
  - Call CreateDepositInvoiceAsync twice for same contract
  - Verify: Second call returns null (idempotent)
  
- [ ] **Test: Payment exceeds AmountDue**
  - Attempt to record payment > AmountDue
  - Verify: Validation error
  
- [ ] **Test: Cancel paid invoice**
  - Attempt to cancel invoice with Status = Paid
  - Verify: CanCancel = false
  
- [ ] **Test: Invoice number collision**
  - Create multiple invoices at same time (concurrent)
  - Verify: Unique invoice numbers generated
  
- [ ] **Test: Missing DepositPolicy**
  - Create deposit invoice for vehicle type without policy
  - Verify: Error handling, log error

### 5.3 UI Testing ❌
- [ ] **Test Index page**
  - Filter by status: Verify results
  - Filter by type: Verify results
  - Filter by date range: Verify results
  - Search by invoice number: Verify results
  - Search by customer name: Verify results
  - Pagination: Click through pages
  
- [ ] **Test Details page**
  - View deposit invoice: Check all fields
  - View rental invoice: Check breakdown display
  - View surcharge invoice: Check line items
  - View refund invoice: Check negative amounts
  - Click Print button: Open print view
  - Click RecordPayment button: Open payment form
  
- [ ] **Test Print page**
  - Open print view: Verify auto-print triggered
  - Check print layout: No navigation, clean format
  - Verify company info: Correct details
  - Verify customer info: Correct details
  - Verify line items: All displayed
  - Verify amounts: Correct calculations
  
- [ ] **Test RecordPayment page**
  - Form validation: Empty amount
  - Form validation: Negative amount
  - Form validation: Amount > AmountDue
  - Quick buttons: Full amount, 50%, Clear
  - Submit: Success message and redirect
  - Submit: Error handling
  
- [ ] **Test ByContract page**
  - View timeline: All 4 invoices displayed
  - View table: All invoices listed
  - Check total row: Correct sums
  - Click Details: Navigate to invoice

---

## 📊 PROGRESS SUMMARY

### Overall Status: 67% Complete
- ✅ Phase 1: Database Layer - **100% DONE**
- ✅ Phase 2: Business Logic Layer - **100% DONE**
- ✅ Phase 3: UI Layer - **100% DONE**
- ❌ Phase 4: Integration - **0% PENDING**
- ❌ Phase 5: Testing - **0% PENDING**

### Statistics:
- **Enums:** 4 files, 34 enum values
- **Models:** 3 files, 46 properties total
- **Database Tables:** 3 tables created
- **Service Methods:** 15 methods, 730+ lines
- **Controller Actions:** 6 actions, 400+ lines
- **ViewModels:** 6 classes, 300+ lines
- **Views:** 5 files, comprehensive UI
- **Bug Fixes:** 9 issues resolved

### Next Steps:
1. **Phase 4.1:** Add CreateDepositInvoiceAsync call in ContractController
2. **Phase 4.2:** Add CreateRentalInvoiceAsync call in HandoverController
3. **Phase 4.3:** Add CreateSurchargePenaltyInvoiceAsync call in ReturnController
4. **Phase 4.4:** Setup Hangfire for background jobs
5. **Phase 5:** Full workflow testing

---

## 🔗 DEPENDENCIES

### Module 3 Dependencies (All Confirmed):
- ✅ `DepositPolicy` table
  - ResponsibilityDepositAmount
  - RentalDepositCalculationType
  - RentalDepositValue
  - RentalDepositMinimum
  - RentalDepositMaximum
  - RefundProcessingDays
  
- ✅ `SurchargePolicy` table
  - VehicleTypeId
  - OvertimeFee (CalculationType, Value)
  - ExtraKilometer (CalculationType, Value)
  - CleaningFee (CalculationType, Value)
  - FuelShortage (CalculationType, Value)
  
- ✅ `Price` table
  - BaseDailyPrice
  - MonthlyMultiplier
  - PeakDayMultiplier
  - OvertimeHourlyPrice
  
- ✅ `ContractPriceBreakdown` table
  - PriceType (Base, Peak, Monthly, Overtime)
  - Days
  - UnitPrice
  - TotalAmount

### Other Module Dependencies:
- `RentalContract` (Module existing)
- `ContractCharge` (Module existing)
- `Customer` → `UserAccount` (Module existing)
- `HandoverRecord` (Module existing)
- `ReturnRecord` (Module existing)
- `Vehicle` → `VehicleModel` (Module existing)
- `StaffProfile` (Module existing)

---

## 📝 NOTES & LESSONS LEARNED

### Design Decisions:
1. **Enum as String in DB:** Chosen for readability and maintainability
   - Pros: Easy to query, human-readable, no breaking changes when adding values
   - Cons: Slightly more storage space
   
2. **DeleteBehavior.Restrict:** Used for all Invoice FK to prevent accidental cascades
   - Reason: SQL Server limitation on multiple cascade paths
   - Benefit: Safer data integrity, explicit delete control
   
3. **InvoiceNumber Format:** PREFIX-YYMM-0001
   - Benefit: Easy to identify type at a glance
   - Benefit: Monthly numbering resets prevent huge numbers
   - Thread-safe: Uses transaction for increment
   
4. **Snapshot Pricing:** Store breakdown in RentalContract, not recalculate
   - Reason: Prices may change over time, need to preserve contract prices
   - Benefit: Historical accuracy
   
5. **Negative Line Items:** Used for rental deposit subtraction and discounts
   - Reason: Show deductions clearly in breakdown
   - Benefit: Transparent for customer

### Common Pitfalls Avoided:
1. ✅ Navigation property names (Vehicle.Model not VehicleModel)
2. ✅ Nullable properties (Customer.UserAccount?.Phone)
3. ✅ Enum value names (ChargeType.OvertimeFee not Overtime)
4. ✅ Cascade delete conflicts (use Restrict)
5. ✅ Invoice number uniqueness (use unique index)
6. ✅ Idempotent invoice creation (check existing before create)
7. ✅ Amount calculation precision (use decimal throughout)
8. ✅ ViewModel property mapping (match exactly between Controller and View)

### Performance Considerations:
- Index on InvoiceNumber for fast lookup
- Include navigation properties in queries to avoid N+1
- Pagination on Index (15 items per page)
- Background services run at off-peak hours (2 AM, hourly)

### Security Considerations:
- Authorization: All actions require Admin/BranchManager/Staff role
- InternalNotes: Only show to staff users
- StaffId from claims: Validated before use
- Anti-forgery token: On POST actions

---

**Last Updated:** 03/02/2025 23:45
**Next Review:** After Phase 4.1 implementation
