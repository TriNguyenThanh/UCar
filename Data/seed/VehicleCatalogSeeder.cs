using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Vehicle Types, Models, and Vehicles
/// </summary>
public static class VehicleCatalogSeeder
{
    public static async Task<(List<VehicleType> types, List<VehicleModel> models, List<Vehicle> vehicles)>
        SeedVehiclesAsync(UCarDbContext context, List<Branch> branches)
    {
        Console.WriteLine("Seeding Vehicle Types, Models, and Vehicles...");

        // Vehicle Types
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

        // Vehicle Models
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

        // Vehicles
        var vehicles = new List<Vehicle>();
        var plateNumbers = GeneratePlateNumbers(50);
        var colors = new[] { "Trắng", "Đen", "Bạc", "Xám", "Đỏ", "Xanh dương", "Xanh lá" };
        var random = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            var model = vehicleModels[random.Next(vehicleModels.Count)];
            var branch = branches[random.Next(branches.Count)];
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
                CurrentStatus = VehicleStatus.Available,
                CurrentOdoKm = odo
            });
        }

        await context.Vehicles.AddRangeAsync(vehicles);
        await context.SaveChangesAsync();

        // Vehicle Status Histories
        var statusHistories = vehicles.Select(v => new VehicleStatusHistory
        {
            VshId = Guid.NewGuid(),
            VehicleId = v.VehicleId,
            FromStatus = null,
            ToStatus = v.CurrentStatus.ToString(),
            ChangedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
            ChangedBy = Guid.Empty,
            Note = "Khởi tạo xe trong hệ thống"
        }).ToList();

        await context.VehicleStatusHistories.AddRangeAsync(statusHistories);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {vehicleTypes.Count} types, {vehicleModels.Count} models, {vehicles.Count} vehicles");
        return (vehicleTypes, vehicleModels, vehicles);
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
