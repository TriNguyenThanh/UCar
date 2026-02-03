using Microsoft.EntityFrameworkCore;
using UCar.Models;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Prices and Holiday Configuration (Module 3.0)
/// </summary>
public static class PricingSeeder
{
    public static async Task SeedPricesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, List<VehicleModel> vehicleModels)
    {
        Console.WriteLine("Seeding Prices...");

        // Price now based on VehicleModel, not VehicleType
        // Get all vehicle models
        var models = vehicleModels.ToDictionary(m => $"{m.Make}_{m.ModelName}", m => m);

        var prices = new List<Price>
        {
            // TOYOTA MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Vios"].ModelId,
                Name = "Toyota Vios - Giá chuẩn",
                BaseDailyPrice = 500000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Corolla Altis"].ModelId,
                Name = "Toyota Corolla Altis - Giá chuẩn",
                BaseDailyPrice = 650000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 55000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Camry"].ModelId,
                Name = "Toyota Camry - Giá chuẩn",
                BaseDailyPrice = 900000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Fortuner"].ModelId,
                Name = "Toyota Fortuner - Giá chuẩn",
                BaseDailyPrice = 1200000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 100000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Cross"].ModelId,
                Name = "Toyota Cross - Giá chuẩn",
                BaseDailyPrice = 850000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Toyota_Innova"].ModelId,
                Name = "Toyota Innova - Giá chuẩn",
                BaseDailyPrice = 800000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // HONDA MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Honda_City"].ModelId,
                Name = "Honda City - Giá chuẩn",
                BaseDailyPrice = 480000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Honda_Civic"].ModelId,
                Name = "Honda Civic - Giá chuẩn",
                BaseDailyPrice = 700000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 60000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Honda_Accord"].ModelId,
                Name = "Honda Accord - Giá chuẩn",
                BaseDailyPrice = 950000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 75000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Honda_CR-V"].ModelId,
                Name = "Honda CR-V - Giá chuẩn",
                BaseDailyPrice = 1100000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 90000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Honda_HR-V"].ModelId,
                Name = "Honda HR-V - Giá chuẩn",
                BaseDailyPrice = 900000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 75000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // HYUNDAI MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Hyundai_i10"].ModelId,
                Name = "Hyundai i10 - Giá chuẩn",
                BaseDailyPrice = 400000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.4m,
                OvertimeHourlyPrice = 40000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Hyundai_Accent"].ModelId,
                Name = "Hyundai Accent - Giá chuẩn",
                BaseDailyPrice = 480000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Hyundai_Elantra"].ModelId,
                Name = "Hyundai Elantra - Giá chuẩn",
                BaseDailyPrice = 650000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 55000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Hyundai_Tucson"].ModelId,
                Name = "Hyundai Tucson - Giá chuẩn",
                BaseDailyPrice = 950000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 80000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Hyundai_Santa Fe"].ModelId,
                Name = "Hyundai Santa Fe - Giá chuẩn",
                BaseDailyPrice = 1300000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 100000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // MAZDA MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Mazda_Mazda 3"].ModelId,
                Name = "Mazda 3 - Giá chuẩn",
                BaseDailyPrice = 700000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 60000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Mazda_Mazda 6"].ModelId,
                Name = "Mazda 6 - Giá chuẩn",
                BaseDailyPrice = 850000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Mazda_CX-5"].ModelId,
                Name = "Mazda CX-5 - Giá chuẩn",
                BaseDailyPrice = 1000000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 80000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Mazda_CX-8"].ModelId,
                Name = "Mazda CX-8 - Giá chuẩn",
                BaseDailyPrice = 1150000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 90000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // FORD MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Ford_Ranger"].ModelId,
                Name = "Ford Ranger - Giá chuẩn",
                BaseDailyPrice = 1300000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 110000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Ford_Everest"].ModelId,
                Name = "Ford Everest - Giá chuẩn",
                BaseDailyPrice = 1400000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 120000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Ford_Territory"].ModelId,
                Name = "Ford Territory - Giá chuẩn",
                BaseDailyPrice = 950000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 80000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // KIA MODELS
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Kia_Morning"].ModelId,
                Name = "Kia Morning - Giá chuẩn",
                BaseDailyPrice = 450000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.4m,
                OvertimeHourlyPrice = 40000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Kia_K3"].ModelId,
                Name = "Kia K3 - Giá chuẩn",
                BaseDailyPrice = 600000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 55000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Kia_K5"].ModelId,
                Name = "Kia K5 - Giá chuẩn",
                BaseDailyPrice = 750000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 60000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Kia_Seltos"].ModelId,
                Name = "Kia Seltos - Giá chuẩn",
                BaseDailyPrice = 850000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["Kia_Carnival"].ModelId,
                Name = "Kia Carnival - Giá chuẩn",
                BaseDailyPrice = 1500000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 120000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            
            // VINFAST MODELS - Xe điện
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["VinFast_VF5"].ModelId,
                Name = "VinFast VF5 - Giá chuẩn",
                BaseDailyPrice = 550000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["VinFast_VF8"].ModelId,
                Name = "VinFast VF8 - Giá chuẩn",
                BaseDailyPrice = 1200000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 100000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = models["VinFast_VF9"].ModelId,
                Name = "VinFast VF9 - Giá chuẩn",
                BaseDailyPrice = 1600000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.7m,
                OvertimeHourlyPrice = 130000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            }
        };

        await context.Prices.AddRangeAsync(prices);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {prices.Count} price entries for vehicle models");
    }

    public static async Task SeedHolidaysAsync(UCarDbContext context, UserAccount adminUser)
    {
        Console.WriteLine("Seeding Holiday Configuration...");

        var holidays = new List<HolidayConfig>
        {
            // Tết Nguyên Đán 2026
            new()
            {
                HolidayId = Guid.NewGuid(),
                HolidayName = "Tết Nguyên Đán 2026",
                StartDate = new DateTime(2026, 1, 29),
                EndDate = new DateTime(2026, 2, 6),
                Year = 2026,
                IsActive = true,
                CreatedBy = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            },
            // Giỗ tổ Hùng Vương 2026
            new()
            {
                HolidayId = Guid.NewGuid(),
                HolidayName = "Giỗ tổ Hùng Vương 2026",
                StartDate = new DateTime(2026, 4, 18),
                EndDate = new DateTime(2026, 4, 18),
                Year = 2026,
                IsActive = true,
                CreatedBy = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            },
            // 30/4 - Giải phóng miền Nam
            new()
            {
                HolidayId = Guid.NewGuid(),
                HolidayName = "Ngày Giải phóng miền Nam",
                StartDate = new DateTime(2026, 4, 30),
                EndDate = new DateTime(2026, 5, 3),
                Year = 2026,
                IsActive = true,
                CreatedBy = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            },
            // Quốc khánh 2/9
            new()
            {
                HolidayId = Guid.NewGuid(),
                HolidayName = "Quốc khánh 2/9",
                StartDate = new DateTime(2026, 9, 2),
                EndDate = new DateTime(2026, 9, 2),
                Year = 2026,
                IsActive = true,
                CreatedBy = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            },
            // Tết Dương lịch 2027 (để test cross-year)
            new()
            {
                HolidayId = Guid.NewGuid(),
                HolidayName = "Tết Dương lịch 2027",
                StartDate = new DateTime(2027, 1, 1),
                EndDate = new DateTime(2027, 1, 3),
                Year = 2027,
                IsActive = true,
                CreatedBy = adminUser.UserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.HolidayConfigs.AddRangeAsync(holidays);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {holidays.Count} holiday configurations");
    }
}
