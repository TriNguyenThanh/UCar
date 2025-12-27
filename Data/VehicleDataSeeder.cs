using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

/// <summary>
/// Seed data cho module Quản lý xe (2.0)
/// </summary>
public static class VehicleDataSeeder
{
    public static async Task SeedVehicleDataAsync(UCarDbContext context, bool force = false)
    {
        // Only seed if no data exists (or force is true)
        if (await context.VehicleTypes.AnyAsync())
        {
            if (!force)
            {
                Console.WriteLine("VehicleDataSeeder: Data already exists. Skipping seed.");
                return;
            }
            
            // Clear existing data in reverse order of dependencies
            Console.WriteLine("VehicleDataSeeder: Force mode - clearing existing data...");
            context.VehicleStatusHistories.RemoveRange(context.VehicleStatusHistories);
            context.Vehicles.RemoveRange(context.Vehicles);
            context.VehicleModels.RemoveRange(context.VehicleModels);
            context.VehicleTypes.RemoveRange(context.VehicleTypes);
            await context.SaveChangesAsync();
        }
        
        Console.WriteLine("VehicleDataSeeder: Seeding data...");

        // 1. Seed Vehicle Types
        var vehicleTypes = new List<VehicleType>
        {
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "Sedan" },
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "SUV" },
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "Hatchback" },
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "MPV" },
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "Pickup" },
            new() { VehicleTypeId = Guid.NewGuid(), TypeName = "Crossover" }
        };

        await context.VehicleTypes.AddRangeAsync(vehicleTypes);
        await context.SaveChangesAsync();

        var sedanType = vehicleTypes[0];
        var suvType = vehicleTypes[1];
        var hatchbackType = vehicleTypes[2];
        var mpvType = vehicleTypes[3];
        var pickupType = vehicleTypes[4];
        var crossoverType = vehicleTypes[5];

        // 2. Seed Vehicle Models
        var vehicleModels = new List<VehicleModel>
        {
            // Toyota
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Toyota", ModelName = "Camry", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Toyota", ModelName = "Corolla Altis", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Toyota", ModelName = "Vios", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "Toyota", ModelName = "Fortuner", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Dầu" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Toyota", ModelName = "Cross", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = mpvType.VehicleTypeId, Make = "Toyota", ModelName = "Innova", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // Honda
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Honda", ModelName = "Civic", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Honda", ModelName = "Accord", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Honda", ModelName = "City", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "Honda", ModelName = "CR-V", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Honda", ModelName = "HR-V", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // Hyundai
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Hyundai", ModelName = "Accent", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Hyundai", ModelName = "Elantra", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "Hyundai", ModelName = "Santa Fe", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Dầu" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Hyundai", ModelName = "Tucson", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = hatchbackType.VehicleTypeId, Make = "Hyundai", ModelName = "i10", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // Mazda
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Mazda", ModelName = "Mazda 3", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Mazda", ModelName = "Mazda 6", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Mazda", ModelName = "CX-5", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "Mazda", ModelName = "CX-8", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // Ford
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = pickupType.VehicleTypeId, Make = "Ford", ModelName = "Ranger", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Dầu" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "Ford", ModelName = "Everest", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Dầu" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Ford", ModelName = "Territory", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // Kia
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Kia", ModelName = "K3", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = sedanType.VehicleTypeId, Make = "Kia", ModelName = "K5", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = hatchbackType.VehicleTypeId, Make = "Kia", ModelName = "Morning", Seats = 4, Transmission = TransmissionType.Manual, FuelType = "Xăng" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = mpvType.VehicleTypeId, Make = "Kia", ModelName = "Carnival", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Dầu" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = crossoverType.VehicleTypeId, Make = "Kia", ModelName = "Seltos", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Xăng" },
            
            // VinFast
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "VinFast", ModelName = "VF8", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Điện" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = suvType.VehicleTypeId, Make = "VinFast", ModelName = "VF9", Seats = 7, Transmission = TransmissionType.Auto, FuelType = "Điện" },
            new() { ModelId = Guid.NewGuid(), VehicleTypeId = hatchbackType.VehicleTypeId, Make = "VinFast", ModelName = "VF5", Seats = 5, Transmission = TransmissionType.Auto, FuelType = "Điện" }
        };

        await context.VehicleModels.AddRangeAsync(vehicleModels);
        await context.SaveChangesAsync();

        // 3. Seed Branches (if not exists)
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Quận 1", Address = "123 Nguyễn Huệ, Quận 1, TP.HCM", PhoneContact = "028-1234-5678" },
                new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Quận 7", Address = "456 Nguyễn Văn Linh, Quận 7, TP.HCM", PhoneContact = "028-2345-6789" },
                new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Thủ Đức", Address = "789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM", PhoneContact = "028-3456-7890" },
                new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Tân Bình", Address = "321 Cộng Hòa, Quận Tân Bình, TP.HCM", PhoneContact = "028-4567-8901" },
                new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Bình Thạnh", Address = "654 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM", PhoneContact = "028-5678-9012" }
            };

            await context.Branches.AddRangeAsync(branches);
            await context.SaveChangesAsync();
        }

        var allBranches = await context.Branches.ToListAsync();
        var random = new Random(42); // Fixed seed for reproducibility

        // 4. Seed Vehicles
        var vehicles = new List<Vehicle>();
        var plateNumbers = GeneratePlateNumbers(50);
        var colors = new[] { "Trắng", "Đen", "Bạc", "Xám", "Đỏ", "Xanh dương", "Xanh lá" };
        var statuses = new[] { VehicleStatus.Available, VehicleStatus.Available, VehicleStatus.Available, 
                               VehicleStatus.Renting, VehicleStatus.Maintenance, VehicleStatus.Reserved };

        for (int i = 0; i < 50; i++)
        {
            var model = vehicleModels[random.Next(vehicleModels.Count)];
            var branch = allBranches[random.Next(allBranches.Count)];
            var status = statuses[random.Next(statuses.Length)];
            var year = random.Next(2019, 2025);
            var odo = random.Next(5000, 80000);

            vehicles.Add(new Vehicle
            {
                VehicleId = Guid.NewGuid(),
                ModelId = model.ModelId,
                BranchId = branch.BranchId,
                PlateNo = plateNumbers[i],
                Color = colors[random.Next(colors.Length)],
                ManufactureYear = year,
                CurrentStatus = status,
                CurrentOdoKm = odo
            });
        }

        await context.Vehicles.AddRangeAsync(vehicles);
        await context.SaveChangesAsync();

        // 5. Seed Status History for each vehicle
        var userId = Guid.Empty; // System user
        var statusHistories = vehicles.Select(v => new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = v.VehicleId,
            FromStatus = null,
            ToStatus = v.CurrentStatus.ToString(),
            ChangedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
            ChangedBy = userId,
            Note = "Khởi tạo xe trong hệ thống"
        }).ToList();

        await context.VehicleStatusHistories.AddRangeAsync(statusHistories);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"VehicleDataSeeder: Successfully seeded {vehicleTypes.Count} types, {vehicleModels.Count} models, {vehicles.Count} vehicles.");
    }

    private static List<string> GeneratePlateNumbers(int count)
    {
        var plates = new List<string>();
        var prefixes = new[] { "51A", "51B", "51C", "51D", "51E", "51F", "51G", "51H", "51K", "30A", "30B", "30C", "29A", "29B" };
        var random = new Random(123);
        var usedNumbers = new HashSet<string>();

        while (plates.Count < count)
        {
            var prefix = prefixes[random.Next(prefixes.Length)];
            var number = random.Next(10000, 99999).ToString();
            var plate = $"{prefix}-{number}";
            
            if (usedNumbers.Add(plate))
            {
                plates.Add(plate);
            }
        }

        return plates;
    }
}
