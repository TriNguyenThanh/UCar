# 📊 PHÂN TÍCH KIẾN TRÚC DỰ ÁN UCAR

## 🏗️ 1. KIẾN TRÚC TỔNG QUAN

### Pattern sử dụng:
- **Architecture**: ASP.NET Core MVC với Repository + Service Pattern
- **Database**: SQL Server với Entity Framework Core 8.0
- **Authentication**: Cookie-based Authentication
- **Dependency Injection**: Built-in ASP.NET Core DI

---

## 📁 2. CẤU TRÚC THƯ MỤC

```
UCar/
├── Controllers/          # MVC Controllers với [Authorize] attributes
├── Models/              # Domain entities (EF Core entities)
│   ├── Enums/          # Enum definitions (chuyển đổi sang string trong DB)
│   └── DTOs/           # Data Transfer Objects (nếu có)
├── ViewModels/         # ViewModels cho Views (suffix: ViewModel)
│   ├── Booking/        # ViewModels theo feature
│   └── Contract/
├── Services/           # Business logic implementation
├── Interfaces/         # Service interfaces (prefix: I)
├── Data/              # DbContext và Data seeders
├── Views/             # Razor Views theo Controller name
└── wwwroot/           # Static files (CSS, JS, images)
```

---

## 🎯 3. PATTERNS & CONVENTIONS

### 3.1. Naming Conventions

#### Models (Entities):
```csharp
// Singular noun, PascalCase
public class Customer { }
public class PaymentTransaction { }
public class RentalContract { }
```

#### ViewModels:
```csharp
// Feature + Purpose + "ViewModel" suffix
public class CustomerListViewModel { }
public class CustomerDetailsViewModel { }
public class CustomerCreateViewModel { }
public class CustomerSearchViewModel { }
```

#### Services:
```csharp
// Interface: I + Feature + "Service"
public interface ICustomerService { }

// Implementation: Feature + "Service"
public class CustomerService : ICustomerService { }
```

#### Controllers:
```csharp
// Feature + "Controller"
public class CustomerController : Controller { }
public class PaymentController : Controller { }
```

### 3.2. Entity Design Pattern

```csharp
public class EntityName
{
    // 1. Primary Key - Guid với suffix "Id"
    [Key]
    public Guid EntityId { get; set; }
    
    // 2. Foreign Keys - Guid với suffix "Id" + [Required]
    [Required]
    public Guid RelatedEntityId { get; set; }
    
    // 3. Properties - Data Annotations cho validation
    [Required]
    [MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;
    
    // 4. Decimal với [Column(TypeName = "decimal(18,2)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    // 5. Enum properties - Convert to string trong OnModelCreating
    [Required]
    public EnumType Status { get; set; }
    
    // 6. Nullable DateTime cho optional dates
    public DateTime? CompletedAt { get; set; }
    
    // 7. Navigation Properties - Cuối cùng với [ForeignKey]
    [ForeignKey(nameof(RelatedEntityId))]
    public RelatedEntity RelatedEntity { get; set; } = null!;
    
    // 8. Collections - Initialize với new List<>()
    public ICollection<ChildEntity> Children { get; set; } = new List<ChildEntity>();
}
```

### 3.3. Service Pattern

```csharp
// Interface
public interface IFeatureService
{
    Task<PaginatedList<ViewModel>> GetListAsync(SearchViewModel search);
    Task<DetailsViewModel?> GetDetailsAsync(Guid id);
    Task<Result> CreateAsync(CreateViewModel model);
    Task<Result> UpdateAsync(Guid id, UpdateViewModel model);
    Task<bool> DeleteAsync(Guid id);
}

// Implementation
public class FeatureService : IFeatureService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<FeatureService> _logger;
    
    public FeatureService(UCarDbContext context, ILogger<FeatureService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Methods with try-catch and logging
    public async Task<PaginatedList<ViewModel>> GetListAsync(SearchViewModel search)
    {
        try
        {
            _logger.LogInformation("Method called with parameters");
            
            var query = _context.Entities
                .Include(e => e.NavigationProperty)
                .AsQueryable();
                
            // Apply filters, sorting, pagination
            // ...
            
            return await PaginatedList<ViewModel>.CreateAsync(query, search.PageNumber, search.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error message");
            throw;
        }
    }
}
```

### 3.4. Controller Pattern

```csharp
[Authorize(Roles = "Admin,Staff")]
public class FeatureController : Controller
{
    private readonly IFeatureService _service;
    private readonly ILogger<FeatureController> _logger;
    
    public FeatureController(IFeatureService service, ILogger<FeatureController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    // GET: /Feature/Index
    [HttpGet]
    public async Task<IActionResult> Index(SearchViewModel search)
    {
        try
        {
            var result = await _service.GetListAsync(search);
            ViewData["CurrentSearch"] = search.SearchTerm;
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error message");
            TempData["ErrorMessage"] = "Có lỗi xảy ra";
            return View(new PaginatedList<ViewModel>(...));
        }
    }
    
    // GET: /Feature/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    
    // POST: /Feature/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        try
        {
            var result = await _service.CreateAsync(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Tạo thành công";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating");
            ModelState.AddModelError("", "Có lỗi xảy ra");
            return View(model);
        }
    }
}
```

---

## 📊 4. DATABASE CONVENTIONS

### 4.1. Enums - Convert to String

```csharp
// In OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Entity>()
        .Property(e => e.Status)
        .HasConversion<string>();
}
```

### 4.2. Unique Constraints

```csharp
modelBuilder.Entity<Entity>()
    .HasIndex(e => e.UniqueProperty)
    .IsUnique();
```

### 4.3. Relationships

```csharp
// One-to-Many
modelBuilder.Entity<Child>()
    .HasOne(c => c.Parent)
    .WithMany(p => p.Children)
    .HasForeignKey(c => c.ParentId)
    .OnDelete(DeleteBehavior.Restrict); // or Cascade
```

---

## 🎨 5. VIEWMODEL CONVENTIONS

### 5.1. List ViewModel
```csharp
public class EntityListViewModel
{
    public Guid EntityId { get; set; }
    
    [Display(Name = "Tiếng Việt")]
    public string PropertyName { get; set; } = string.Empty;
    
    // Chỉ properties cần thiết cho list view
}
```

### 5.2. Search/Filter ViewModel
```csharp
public class EntitySearchViewModel
{
    [Display(Name = "Tìm kiếm")]
    public string? SearchTerm { get; set; }
    
    public string? StatusFilter { get; set; }
    
    public string? SortBy { get; set; }
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
}
```

### 5.3. Paginated List
```csharp
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
```

---

## 📝 6. DFD MAPPING COMMENTS

```csharp
/// <summary>
/// Feature service implementation
/// Implements business logic according to DFD X.Y
/// </summary>

#region DFD X.Y: Feature Name - Description
// Method implementation
#endregion
```

---

## 🔧 7. DEPENDENCY REGISTRATION (Program.cs)

```csharp
// Register services in Program.cs
builder.Services.AddScoped<IFeatureService, FeatureService>();
```

---

## 📋 8. ENTITIES HIỆN CÓ LIÊN QUAN ĐÃN MODULES 3, 7, 9

### Module 3 (Pricing) - ĐÃ CÓ:
- ✅ `Price` - Entity cho bảng giá
- ✅ `VehicleType` - Loại xe

### Module 7 (Payment) - ĐÃ CÓ:
- ✅ `PaymentTransaction` - Giao dịch thanh toán
- ✅ `RentalContract` - Hợp đồng (có TotalAmountFinal)
- ✅ Enums: `TransactionType`, `PaymentMethod`, `TransactionStatus`

### Module 9 (Reports) - CẦN TẠO:
- ❌ ReportData models
- ❌ Report DTOs

---

## ✅ 9. NEXT STEPS

1. Tạo Models mới cho Module 3, 7, 9
2. Tạo Interfaces cho Services
3. Implement Services
4. Tạo Controllers
5. Tạo ViewModels
6. Tạo Views
7. Register services trong Program.cs
8. Tạo migrations và update database
