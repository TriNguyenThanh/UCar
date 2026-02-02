# TODO: PRICING SYSTEM REFACTOR - COMPLETE ROADMAP

## 🎯 OBJECTIVE
Refactor hệ thống Price từ VehicleType-based sang VehicleModel-based với cơ chế snapshot và holiday pricing.

---

## ⚠️ CRITICAL: DATABASE RESET
**DROP tất cả migrations và tạo lại từ đầu để tránh conflict**

- [ ] **TASK 0.1**: Backup database hiện tại (nếu cần)
- [ ] **TASK 0.2**: Drop database `UCar`
- [ ] **TASK 0.3**: Xóa tất cả files trong folder `Migrations/`
- [ ] **TASK 0.4**: Tạo migration mới với schema hoàn chỉnh

---

## 📦 PHASE 1: MODEL CHANGES

### TASK 1.1: Refactor Price Model ✅
**File**: `Models/Price.cs`

**Changes**:
```csharp
// BEFORE
public Guid VehicleTypeId { get; set; }
public PriceUnit Unit { get; set; }
public decimal UnitPrice { get; set; }
public decimal OvertimeHourlyPrice { get; set; }
public decimal DepositSuggest { get; set; }

// AFTER
public Guid VehicleModelId { get; set; }  // ← Changed from VehicleTypeId
public decimal BaseDailyPrice { get; set; }  // ← Renamed from UnitPrice
public decimal MonthMultiplier { get; set; }  // ← NEW: Hệ số tháng (0.85)
public decimal PeakMultiplier { get; set; }   // ← NEW: Hệ số lễ (1.5)
public decimal OvertimeHourlyPrice { get; set; }
// Remove: DepositSuggest (move to separate policy)
// Remove: Unit enum
```

**Navigation**:
```csharp
[ForeignKey(nameof(VehicleModelId))]
public VehicleModel VehicleModel { get; set; } = null!;
```

---

### TASK 1.2: Create HolidayConfig Model ✅
**File**: `Models/HolidayConfig.cs`

```csharp
public class HolidayConfig
{
    [Key]
    public Guid HolidayId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string HolidayName { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public int Year { get; set; }
    
    public bool IsActive { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public Guid CreatedBy { get; set; }
}
```

---

### TASK 1.3: Enhance RentalContract Model ✅
**File**: `Models/RentalContract.cs`

**Add Snapshot Fields**:
```csharp
// === SNAPSHOT PRICING ===
[Column(TypeName = "decimal(18,2)")]
public decimal SnapshotBaseDailyPrice { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal SnapshotMonthMultiplier { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal SnapshotPeakMultiplier { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal SnapshotOvertimeHourlyPrice { get; set; }

// === PRICE BREAKDOWN ===
public int NormalDays { get; set; }

public int PeakDays { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal NormalDaysAmount { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal PeakDaysAmount { get; set; }

public bool IsMonthlyRate { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal MonthlyAmount { get; set; }

// === DEPOSIT BREAKDOWN ===
[Column(TypeName = "decimal(18,2)")]
public decimal ResponsibilityDeposit { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal RentalDeposit { get; set; }

public DateTime? DepositRefundDueDate { get; set; }
```

---

### TASK 1.4: Create ContractPriceBreakdown Model (Optional) ✅
**File**: `Models/ContractPriceBreakdown.cs`

```csharp
public class ContractPriceBreakdown
{
    [Key]
    public Guid BreakdownId { get; set; }
    
    [Required]
    public Guid ContractId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string LineDescription { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }
    
    public int DisplayOrder { get; set; }
    
    [ForeignKey(nameof(ContractId))]
    public RentalContract RentalContract { get; set; } = null!;
}
```

---

### TASK 1.5: Update TransactionType Enum ✅
**File**: `Models/Enums/TransactionType.cs`

```csharp
public enum TransactionType
{
    ResponsibilityDeposit,  // Cọc trách nhiệm (fixed 2M)
    RentalDeposit,          // Cọc thuê xe (50%)
    RentalFee,              // Tiền thuê xe
    Penalty,                // Phạt vi phạm
    RefundResponsibility,   // Hoàn cọc trách nhiệm
    RefundRental            // Hoàn cọc thuê
}
```

---

## 🔧 PHASE 2: UPDATE DbContext

### TASK 2.1: Update UCarDbContext ✅
**File**: `Data/UCarDbContext.cs`

**Add DbSets**:
```csharp
public DbSet<HolidayConfig> HolidayConfigs { get; set; }
public DbSet<ContractPriceBreakdown> ContractPriceBreakdowns { get; set; }
```

**Update Configurations**:
```csharp
// Price -> VehicleModel (not VehicleType)
modelBuilder.Entity<Price>()
    .HasOne(p => p.VehicleModel)
    .WithMany()
    .HasForeignKey(p => p.VehicleModelId)
    .OnDelete(DeleteBehavior.Restrict);

// HolidayConfig index
modelBuilder.Entity<HolidayConfig>()
    .HasIndex(h => new { h.Year, h.IsActive });

// ContractPriceBreakdown
modelBuilder.Entity<ContractPriceBreakdown>()
    .HasOne(cpb => cpb.RentalContract)
    .WithMany()
    .HasForeignKey(cpb => cpb.ContractId)
    .OnDelete(DeleteBehavior.Cascade);
```

---

## 🔨 PHASE 3: UPDATE SERVICES & INTERFACES

### TASK 3.1: Update IPricingService ✅
**File**: `Interfaces/IPricingService.cs`

**Replace**:
```csharp
// OLD
Task<Price?> GetActivePriceByVehicleTypeAsync(Guid vehicleTypeId);

// NEW
Task<Price?> GetActivePriceByVehicleModelAsync(Guid vehicleModelId);
Task<PagedResult<Price>> GetPricesAsync(Guid? vehicleModelId = null, bool? isActive = null, int page = 1, int pageSize = 20);
```

---

### TASK 3.2: Create IPriceCalculationService ✅
**File**: `Interfaces/IPriceCalculationService.cs`

```csharp
public interface IPriceCalculationService
{
    Task<PriceEstimateDto> CalculateEstimateAsync(
        Guid vehicleModelId, 
        DateTime startDate, 
        DateTime endDate
    );
    
    Task<Price?> GetCurrentPriceByModelAsync(Guid vehicleModelId);
    
    Task<ContractPriceDetails> GenerateContractPriceDetailsAsync(
        Guid vehicleModelId,
        DateTime startDate,
        DateTime endDate
    );
}
```

---

### TASK 3.3: Create IHolidayService ✅
**File**: `Interfaces/IHolidayService.cs`

```csharp
public interface IHolidayService
{
    Task<List<HolidayConfig>> GetHolidaysInRangeAsync(DateTime start, DateTime end);
    Task<int> CountHolidayDaysAsync(DateTime start, DateTime end);
    Task<bool> IsHolidayAsync(DateTime date);
    Task<HolidayConfig> CreateHolidayAsync(HolidayConfig holiday);
    Task<HolidayConfig> UpdateHolidayAsync(HolidayConfig holiday);
    Task DeleteHolidayAsync(Guid holidayId);
    Task<List<HolidayConfig>> GetAllHolidaysByYearAsync(int year);
}
```

---

### TASK 3.4: Update IDepositPolicyService ✅
**File**: `Interfaces/IDepositPolicyService.cs`

**Change all**:
```csharp
// OLD
Task<DepositPolicy?> GetActiveDepositPolicyByVehicleTypeAsync(Guid vehicleTypeId);
Task<decimal> CalculateDepositAmountAsync(Guid vehicleTypeId, decimal rentalAmount);

// NEW
Task<DepositPolicy?> GetActiveDepositPolicyByVehicleModelAsync(Guid vehicleModelId);
Task<decimal> CalculateDepositAmountAsync(Guid vehicleModelId, decimal rentalAmount);
```

---

### TASK 3.5: Update ISurchargePolicyService ✅
**File**: `Interfaces/ISurchargePolicyService.cs`

**Change all** `vehicleTypeId` → `vehicleModelId`

---

## 💼 PHASE 4: IMPLEMENT SERVICES

### TASK 4.1: Update PricingService ✅
**File**: `Services/PricingService.cs`

- Replace all `VehicleTypeId` → `VehicleModelId`
- Update query logic
- Update validation

---

### TASK 4.2: Implement PriceCalculationService ✅
**File**: `Services/PriceCalculationService.cs`

**Core Logic**:
```csharp
public async Task<PriceEstimateDto> CalculateEstimateAsync(
    Guid vehicleModelId, DateTime startDate, DateTime endDate)
{
    var price = await _pricingService.GetActivePriceByVehicleModelAsync(vehicleModelId);
    if (price == null) throw new InvalidOperationException("Price not found");
    
    var totalDays = (endDate - startDate).Days + 1;
    
    // Case 1: >= 30 days → Monthly rate
    if (totalDays >= 30)
    {
        var monthlyPrice = price.BaseDailyPrice * 30 * price.MonthMultiplier;
        var totalAmount = (monthlyPrice / 30) * totalDays;
        
        return new PriceEstimateDto
        {
            TotalDays = totalDays,
            IsMonthlyRate = true,
            MonthlyPrice = monthlyPrice,
            TotalRentalAmount = totalAmount,
            ResponsibilityDeposit = 2_000_000, // Fixed
            RentalDeposit = totalAmount * 0.5m,
            TotalDepositRequired = 2_000_000 + (totalAmount * 0.5m)
        };
    }
    
    // Case 2: < 30 days → Daily + Peak
    var holidayDays = await _holidayService.CountHolidayDaysAsync(startDate, endDate);
    var normalDays = totalDays - holidayDays;
    
    var normalAmount = normalDays * price.BaseDailyPrice;
    var peakAmount = holidayDays * price.BaseDailyPrice * price.PeakMultiplier;
    var totalRental = normalAmount + peakAmount;
    
    return new PriceEstimateDto
    {
        TotalDays = totalDays,
        NormalDays = normalDays,
        PeakDays = holidayDays,
        IsMonthlyRate = false,
        BaseDailyPrice = price.BaseDailyPrice,
        PeakDailyPrice = price.BaseDailyPrice * price.PeakMultiplier,
        TotalRentalAmount = totalRental,
        ResponsibilityDeposit = 2_000_000,
        RentalDeposit = totalRental * 0.5m,
        TotalDepositRequired = 2_000_000 + (totalRental * 0.5m),
        Breakdown = GenerateBreakdown(normalDays, holidayDays, price)
    };
}
```

---

### TASK 4.3: Implement HolidayService ✅
**File**: `Services/HolidayService.cs`

```csharp
public async Task<int> CountHolidayDaysAsync(DateTime start, DateTime end)
{
    var holidays = await _context.HolidayConfigs
        .Where(h => h.IsActive)
        .Where(h => h.EndDate >= start && h.StartDate <= end)
        .ToListAsync();
    
    int count = 0;
    for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
    {
        if (holidays.Any(h => date >= h.StartDate.Date && date <= h.EndDate.Date))
        {
            count++;
        }
    }
    
    return count;
}
```

---

### TASK 4.4: Update DepositPolicyService ✅
- Change all `VehicleTypeId` → `VehicleModelId`

---

### TASK 4.5: Update SurchargePolicyService ✅
- Change all `VehicleTypeId` → `VehicleModelId`

---

## 🎮 PHASE 5: UPDATE CONTROLLERS

### TASK 5.1: Update PricingController ✅
**File**: `Controllers/PricingController.cs`

- Change all `VehicleTypeId` → `VehicleModelId`
- Update ViewData to use VehicleModels instead of VehicleTypes

---

### TASK 5.2: Update BookingController ✅
**File**: `Controllers/BookingController.cs`

**Add API endpoint**:
```csharp
[HttpPost]
public async Task<JsonResult> CalculatePrice(
    Guid vehicleModelId, DateTime startDate, DateTime endDate)
{
    var estimate = await _priceCalculationService
        .CalculateEstimateAsync(vehicleModelId, startDate, endDate);
    return Json(estimate);
}
```

---

### TASK 5.3: Create HolidayController ✅
**File**: `Controllers/HolidayController.cs`

```csharp
[Authorize(Roles = "Admin,Manager")]
public class HolidayController : Controller
{
    // CRUD operations for holiday management
}
```

---

## 📄 PHASE 6: UPDATE VIEWMODELS

### TASK 6.1: Update Pricing ViewModels ✅
- `PricingCreateViewModel.cs`: VehicleTypeId → VehicleModelId
- `PricingEditViewModel.cs`: VehicleTypeId → VehicleModelId
- `PricingListViewModel.cs`: VehicleTypeId → VehicleModelId

---

### TASK 6.2: Create DTO Models ✅
**File**: `Models/DTOs/PriceEstimateDto.cs`

```csharp
public class PriceEstimateDto
{
    public int TotalDays { get; set; }
    public int NormalDays { get; set; }
    public int PeakDays { get; set; }
    public bool IsMonthlyRate { get; set; }
    public decimal BaseDailyPrice { get; set; }
    public decimal PeakDailyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal TotalRentalAmount { get; set; }
    public decimal ResponsibilityDeposit { get; set; }
    public decimal RentalDeposit { get; set; }
    public decimal TotalDepositRequired { get; set; }
    public List<PriceBreakdownLine> Breakdown { get; set; } = new();
}

public class PriceBreakdownLine
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
```

---

## 🗄️ PHASE 7: DATABASE MIGRATION

### TASK 7.1: Drop & Recreate Database ✅
```bash
# Drop database
Drop-Database

# Delete all migration files
rm Migrations\*.cs

# Create new migration
Add-Migration InitialCreateWithNewPricing

# Apply migration
Update-Database
```

---

### TASK 7.2: Update Data Seeder ✅
**File**: `Data/UCarDataSeeder.cs`

```csharp
// Seed Prices for VehicleModels (not VehicleTypes)
var toyotaVios = vehicleModels.First(vm => vm.ModelName == "Vios");
var hondaCity = vehicleModels.First(vm => vm.ModelName == "City");

var prices = new List<Price>
{
    new Price
    {
        VehicleModelId = toyotaVios.ModelId,
        Name = "Toyota Vios - Giá chuẩn",
        BaseDailyPrice = 500_000,
        MonthMultiplier = 0.85m,
        PeakMultiplier = 1.5m,
        OvertimeHourlyPrice = 60_000,
        IsActive = true,
        ValidFrom = DateTime.Now
    },
    new Price
    {
        VehicleModelId = hondaCity.ModelId,
        Name = "Honda City - Giá chuẩn",
        BaseDailyPrice = 480_000,
        MonthMultiplier = 0.85m,
        PeakMultiplier = 1.5m,
        OvertimeHourlyPrice = 60_000,
        IsActive = true,
        ValidFrom = DateTime.Now
    }
};

// Seed HolidayConfigs
var holidays = new List<HolidayConfig>
{
    new HolidayConfig
    {
        HolidayName = "Tết Nguyên Đán 2026",
        StartDate = new DateTime(2026, 1, 28),
        EndDate = new DateTime(2026, 2, 3),
        Year = 2026,
        IsActive = true,
        CreatedAt = DateTime.Now
    },
    new HolidayConfig
    {
        HolidayName = "Giỗ Tổ Hùng Vương",
        StartDate = new DateTime(2026, 4, 21),
        EndDate = new DateTime(2026, 4, 21),
        Year = 2026,
        IsActive = true,
        CreatedAt = DateTime.Now
    }
};
```

---

## 🖥️ PHASE 8: UPDATE VIEWS

### TASK 8.1: Update Pricing Views ✅
- Change all dropdowns from VehicleTypes to VehicleModels
- Update display to show Model info

---

### TASK 8.2: Create Holiday Views ✅
- `Views/Holiday/Index.cshtml`: List holidays by year
- `Views/Holiday/Create.cshtml`: Create new holiday
- `Views/Holiday/Edit.cshtml`: Edit holiday

---

### TASK 8.3: Update Booking Views ✅
- Add realtime price calculator with AJAX
- Display breakdown (normal days + peak days)

---

## ⚙️ PHASE 9: REGISTER SERVICES

### TASK 9.1: Update Program.cs ✅
```csharp
// Pricing services
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPriceCalculationService, PriceCalculationService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IDepositPolicyService, DepositPolicyService>();
builder.Services.AddScoped<ISurchargePolicyService, SurchargePolicyService>();
```

---

## ✅ PHASE 10: TESTING

### TASK 10.1: Unit Tests
- [ ] Test price calculation < 30 days
- [ ] Test price calculation >= 30 days
- [ ] Test holiday counting
- [ ] Test snapshot generation

---

### TASK 10.2: Integration Tests
- [ ] Create booking with realtime price
- [ ] Generate contract with snapshot
- [ ] Verify price independence
- [ ] Test deposit calculation

---

### TASK 10.3: Manual Testing
- [ ] Create price for Toyota Vios
- [ ] Book from 1/2 to 5/2 (with holidays)
- [ ] Verify price breakdown
- [ ] Change price in admin
- [ ] Verify old contracts unchanged
- [ ] Book again with new price

---

## 📊 PROGRESS TRACKING

**Status Legend**:
- ⬜ Not Started
- 🟨 In Progress
- ✅ Completed
- ❌ Blocked

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 0: DB Reset | ⬜ | Need to drop & recreate |
| Phase 1: Models | ⬜ | |
| Phase 2: DbContext | ⬜ | |
| Phase 3: Interfaces | ⬜ | |
| Phase 4: Services | ⬜ | |
| Phase 5: Controllers | ⬜ | |
| Phase 6: ViewModels | ⬜ | |
| Phase 7: Migration | ⬜ | |
| Phase 8: Views | ⬜ | |
| Phase 9: Registration | ⬜ | |
| Phase 10: Testing | ⬜ | |

---

## 🚀 QUICK START

**Begin with**:
1. Drop database
2. Delete migrations
3. Refactor Price model
4. Create HolidayConfig model
5. Enhance RentalContract model
6. Update DbContext
7. Create new migration
8. Update seeder
9. Apply migration

**Then proceed with services and controllers.**
