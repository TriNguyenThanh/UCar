using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

public static class Tri_seed
{
    public static async Task SeedDataAsync(UCarDbContext context)
    {
        // Check if data already exists
        if (await context.Roles.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Helper method to hash passwords
        string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        // Seed Roles
        var adminRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Admin,
            Name = "Administrator",
            IsActive = true
        };

        var staffRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Staff,
            Name = "Staff",
            IsActive = true
        };

        var customerRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Customer,
            Name = "Customer",
            IsActive = true
        };

        // await context.Roles.AddRangeAsync(adminRole, staffRole, customerRole);
        // await context.SaveChangesAsync();

        // Seed UserAccounts
        var adminUser = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = adminRole.RoleId,
            Username = "admin",
            Email = "admin@ucar.com",
            Phone = "0901234567",
            PasswordHash = HashPassword("admin123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        var staffUser1 = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = staffRole.RoleId,
            Username = "staff01",
            Email = "staff01@ucar.com",
            Phone = "0902345678",
            PasswordHash = HashPassword("staff123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var staffUser2 = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = staffRole.RoleId,
            Username = "staff02",
            Email = "staff02@ucar.com",
            Phone = "0903456789",
            PasswordHash = HashPassword("staff123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customerUser1 = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = customerRole.RoleId,
            Username = "customer01",
            Email = "customer01@gmail.com",
            Phone = "0904567890",
            PasswordHash = HashPassword("customer123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customerUser2 = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = customerRole.RoleId,
            Username = "customer02",
            Email = "customer02@gmail.com",
            Phone = "0905678901",
            PasswordHash = HashPassword("customer123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.UserAccounts.AddRangeAsync(adminUser, staffUser1, staffUser2, customerUser1, customerUser2);
        await context.SaveChangesAsync();

        // Seed Branches
        var branch1 = new Branch
        {
            BranchId = Guid.NewGuid(),
            Name = "UCar Hanoi Main",
            Address = "123 Ba Dinh, Hanoi, Vietnam",
            PhoneContact = "0243456789"
        };

        var branch2 = new Branch
        {
            BranchId = Guid.NewGuid(),
            Name = "UCar Ho Chi Minh",
            Address = "456 District 1, Ho Chi Minh City, Vietnam",
            PhoneContact = "0283456789"
        };

        var branch3 = new Branch
        {
            BranchId = Guid.NewGuid(),
            Name = "UCar Da Nang",
            Address = "789 Hai Chau, Da Nang, Vietnam",
            PhoneContact = "0236456789"
        };

        await context.Branches.AddRangeAsync(branch1, branch2, branch3);
        await context.SaveChangesAsync();

        // Seed StaffProfiles
        var staffProfile1 = new StaffProfile
        {
            StaffId = Guid.NewGuid(),
            UserId = staffUser1.UserId,
            BranchId = branch1.BranchId,
            StaffCode = "ST001",
            FullName = "Nguyen Van A",
            Position = "Rental Specialist",
            IsActive = true
        };

        var staffProfile2 = new StaffProfile
        {
            StaffId = Guid.NewGuid(),
            UserId = staffUser2.UserId,
            BranchId = branch2.BranchId,
            StaffCode = "ST002",
            FullName = "Tran Thi B",
            Position = "Customer Service",
            IsActive = true
        };

        await context.StaffProfiles.AddRangeAsync(staffProfile1, staffProfile2);
        await context.SaveChangesAsync();

        // Seed Customers
        var customer1 = new Customer
        {
            CustomerId = Guid.NewGuid(),
            UserId = customerUser1.UserId,
            FullName = "Pham Van C",
            Dob = new DateTime(1990, 5, 15),
            AddressText = "45 Cau Giay, Hanoi",
            RiskLevel = "Low",
            IsBlacklisted = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        var customer2 = new Customer
        {
            CustomerId = Guid.NewGuid(),
            UserId = customerUser2.UserId,
            FullName = "Le Thi D",
            Dob = new DateTime(1995, 8, 20),
            AddressText = "78 District 3, Ho Chi Minh City",
            RiskLevel = "Medium",
            IsBlacklisted = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        await context.Customers.AddRangeAsync(customer1, customer2);
        await context.SaveChangesAsync();

        // Seed VehicleTypes
        var vehicleTypeSedan = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            TypeName = "Sedan"
        };

        var vehicleTypeSUV = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            TypeName = "SUV"
        };

        var vehicleTypeHatchback = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            TypeName = "Hatchback"
        };

        var vehicleTypeMotorbike = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            TypeName = "Motorbike"
        };

        await context.VehicleTypes.AddRangeAsync(vehicleTypeSedan, vehicleTypeSUV, vehicleTypeHatchback, vehicleTypeMotorbike);
        await context.SaveChangesAsync();

        // Seed VehicleModels
        var modelToyotaVios = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSedan.VehicleTypeId,
            Make = "Toyota",
            ModelName = "Vios",
            Seats = 5,
            Transmission = TransmissionType.Auto,
            FuelType = "Gasoline"
        };

        var modelHondaCivic = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSedan.VehicleTypeId,
            Make = "Honda",
            ModelName = "Civic",
            Seats = 5,
            Transmission = TransmissionType.Auto,
            FuelType = "Gasoline"
        };

        var modelToyotaFortuner = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSUV.VehicleTypeId,
            Make = "Toyota",
            ModelName = "Fortuner",
            Seats = 7,
            Transmission = TransmissionType.Auto,
            FuelType = "Diesel"
        };

        var modelMazdaCX5 = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSUV.VehicleTypeId,
            Make = "Mazda",
            ModelName = "CX-5",
            Seats = 5,
            Transmission = TransmissionType.Auto,
            FuelType = "Gasoline"
        };

        var modelHondaCity = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSedan.VehicleTypeId,
            Make = "Honda",
            ModelName = "City",
            Seats = 5,
            Transmission = TransmissionType.Auto,
            FuelType = "Gasoline"
        };

        var modelHondaAirBlade = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeMotorbike.VehicleTypeId,
            Make = "Honda",
            ModelName = "Air Blade",
            Seats = 2,
            Transmission = TransmissionType.Auto,
            FuelType = "Gasoline"
        };

        await context.VehicleModels.AddRangeAsync(
            modelToyotaVios, modelHondaCivic, modelToyotaFortuner, 
            modelMazdaCX5, modelHondaCity, modelHondaAirBlade
        );
        await context.SaveChangesAsync();

        // Seed Vehicles
        var vehicle1 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch1.BranchId,
            ModelId = modelToyotaVios.ModelId,
            PlateNo = "30A-12345",
            Color = "White",
            ManufactureYear = 2022,
            CurrentStatus = VehicleStatus.Available,
            CurrentOdoKm = 15000
        };

        var vehicle2 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch1.BranchId,
            ModelId = modelHondaCivic.ModelId,
            PlateNo = "30B-23456",
            Color = "Black",
            ManufactureYear = 2023,
            CurrentStatus = VehicleStatus.Available,
            CurrentOdoKm = 8000
        };

        var vehicle3 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch2.BranchId,
            ModelId = modelToyotaFortuner.ModelId,
            PlateNo = "51F-34567",
            Color = "Silver",
            ManufactureYear = 2021,
            CurrentStatus = VehicleStatus.Available,
            CurrentOdoKm = 45000
        };

        var vehicle4 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch2.BranchId,
            ModelId = modelMazdaCX5.ModelId,
            PlateNo = "51G-45678",
            Color = "Red",
            ManufactureYear = 2023,
            CurrentStatus = VehicleStatus.Renting,
            CurrentOdoKm = 12000
        };

        var vehicle5 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch3.BranchId,
            ModelId = modelHondaCity.ModelId,
            PlateNo = "43C-56789",
            Color = "Blue",
            ManufactureYear = 2022,
            CurrentStatus = VehicleStatus.Available,
            CurrentOdoKm = 20000
        };

        var vehicle6 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch1.BranchId,
            ModelId = modelHondaAirBlade.ModelId,
            PlateNo = "30D-67890",
            Color = "Black",
            ManufactureYear = 2023,
            CurrentStatus = VehicleStatus.Available,
            CurrentOdoKm = 5000
        };

        var vehicle7 = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            BranchId = branch1.BranchId,
            ModelId = modelToyotaVios.ModelId,
            PlateNo = "30E-78901",
            Color = "Gray",
            ManufactureYear = 2021,
            CurrentStatus = VehicleStatus.Maintenance,
            CurrentOdoKm = 55000
        };

        await context.Vehicles.AddRangeAsync(
            vehicle1, vehicle2, vehicle3, vehicle4, vehicle5, vehicle6, vehicle7
        );
        await context.SaveChangesAsync();

        // Seed Prices
        var priceSedanDaily = new Price
        {
            PriceId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSedan.VehicleTypeId,
            Name = "Sedan Daily Rate",
            Unit = PriceUnit.Day,
            UnitPrice = 500000,
            OvertimeHourlyPrice = 50000,
            DepositSuggest = 5000000,
            ValidFrom = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        };

        var priceSedanMonthly = new Price
        {
            PriceId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSedan.VehicleTypeId,
            Name = "Sedan Monthly Rate",
            Unit = PriceUnit.Month,
            UnitPrice = 12000000,
            OvertimeHourlyPrice = 50000,
            DepositSuggest = 5000000,
            ValidFrom = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        };

        var priceSUVDaily = new Price
        {
            PriceId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeSUV.VehicleTypeId,
            Name = "SUV Daily Rate",
            Unit = PriceUnit.Day,
            UnitPrice = 900000,
            OvertimeHourlyPrice = 90000,
            DepositSuggest = 10000000,
            ValidFrom = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        };

        var priceMotorbikeDaily = new Price
        {
            PriceId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeMotorbike.VehicleTypeId,
            Name = "Motorbike Daily Rate",
            Unit = PriceUnit.Day,
            UnitPrice = 150000,
            OvertimeHourlyPrice = 15000,
            DepositSuggest = 2000000,
            ValidFrom = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        };

        await context.Prices.AddRangeAsync(
            priceSedanDaily, priceSedanMonthly, priceSUVDaily, priceMotorbikeDaily
        );
        await context.SaveChangesAsync();
    }
}
