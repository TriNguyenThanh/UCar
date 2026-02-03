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
        // Get representative models from each type
        var toyotaVios = vehicleModels.First(m => m.Make == "Toyota" && m.ModelName == "Vios");
        var toyotaCamry = vehicleModels.First(m => m.Make == "Toyota" && m.ModelName == "Camry");
        var toyotaFortuner = vehicleModels.First(m => m.Make == "Toyota" && m.ModelName == "Fortuner");
        var toyotaInnova = vehicleModels.First(m => m.Make == "Toyota" && m.ModelName == "Innova");
        var hondaCity = vehicleModels.First(m => m.Make == "Honda" && m.ModelName == "City");
        var hondaCRV = vehicleModels.First(m => m.Make == "Honda" && m.ModelName == "CR-V");
        var hyundaii10 = vehicleModels.First(m => m.Make == "Hyundai" && m.ModelName == "i10");
        var fordRanger = vehicleModels.First(m => m.Make == "Ford" && m.ModelName == "Ranger");
        var mazda3 = vehicleModels.First(m => m.Make == "Mazda" && m.ModelName == "Mazda 3");
        var mazdaCX5 = vehicleModels.First(m => m.Make == "Mazda" && m.ModelName == "CX-5");

        var prices = new List<Price>
        {
            // Toyota Vios - Sedan phổ thông
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = toyotaVios.ModelId,
                Name = "Toyota Vios - Giá chuẩn",
                BaseDailyPrice = 500000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Toyota Camry - Sedan cao cấp
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = toyotaCamry.ModelId,
                Name = "Toyota Camry - Giá chuẩn",
                BaseDailyPrice = 900000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Toyota Fortuner - SUV
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = toyotaFortuner.ModelId,
                Name = "Toyota Fortuner - Giá chuẩn",
                BaseDailyPrice = 1200000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 100000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Toyota Innova - MPV
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = toyotaInnova.ModelId,
                Name = "Toyota Innova - Giá chuẩn",
                BaseDailyPrice = 800000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 70000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Honda City - Sedan phổ thông
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = hondaCity.ModelId,
                Name = "Honda City - Giá chuẩn",
                BaseDailyPrice = 480000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 50000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Honda CR-V - SUV
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = hondaCRV.ModelId,
                Name = "Honda CR-V - Giá chuẩn",
                BaseDailyPrice = 1100000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 90000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Hyundai i10 - Hatchback
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = hyundaii10.ModelId,
                Name = "Hyundai i10 - Giá chuẩn",
                BaseDailyPrice = 400000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.4m,
                OvertimeHourlyPrice = 40000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Ford Ranger - Pickup
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = fordRanger.ModelId,
                Name = "Ford Ranger - Giá chuẩn",
                BaseDailyPrice = 1300000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.6m,
                OvertimeHourlyPrice = 110000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Mazda 3 - Sedan
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = mazda3.ModelId,
                Name = "Mazda 3 - Giá chuẩn",
                BaseDailyPrice = 700000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 60000,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                IsActive = true
            },
            // Mazda CX-5 - Crossover
            new()
            {
                PriceId = Guid.NewGuid(),
                VehicleModelId = mazdaCX5.ModelId,
                Name = "Mazda CX-5 - Giá chuẩn",
                BaseDailyPrice = 1000000,
                MonthMultiplier = 0.85m,
                PeakMultiplier = 1.5m,
                OvertimeHourlyPrice = 80000,
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
