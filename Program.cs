using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Infrastructure;
using UCar.Interfaces;
using UCar.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Register custom DateTime model binder for all DateTime properties
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
});
builder.Services.AddDbContext<UCarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TriDb"))); //Anh em nhớ đổi chỗ này nhé

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Changed from Always
        options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict
    });

// Register application services
builder.Services.AddHttpContextAccessor(); // Required for IBranchAccessService
builder.Services.AddScoped<IBranchAccessService, BranchAccessService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Register Vehicle Management services
builder.Services.AddScoped<IVehicleStatusService, VehicleStatusService>();
builder.Services.AddScoped<IVehicleCatalogService, VehicleCatalogService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

// Register Handover services
builder.Services.AddScoped<IHandoverService, HandoverService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Register Contract services
builder.Services.AddScoped<IContractService, ContractService>();

// Register Invoice services (Module 7)
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddHostedService<BackgroundInvoiceService>(); // Background service cho tự động xử lý invoice

// Register Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Register Operations & HR services (Module 8.0)
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IOperationalTaskService, OperationalTaskService>();
builder.Services.AddScoped<IShiftService, ShiftService>();

// Register Pricing & Policy services (Module 3.0)
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPriceCalculationService, PriceCalculationService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IDepositPolicyService, DepositPolicyService>();
builder.Services.AddScoped<ISurchargePolicyService, SurchargePolicyService>();

// Document Template Service (DOCX templates)
builder.Services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();

// Image Upload Service
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
// Register Analytics services (Module 9.0)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

var app = builder.Build();

// Seed database with unified seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UCarDbContext>();
        await UCarDataSeeder.SeedAllDataAsync(context, force: false);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();