using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

/// <summary>
/// Unified Data Seeder for UCar - combines all seeders into one
/// Handles: Roles, Users, Branches, Staff, Customers, Vehicles, Prices, Contracts, Handovers, Shifts, Tasks
/// </summary>
public static class UCarDataSeeder
{
    public static async Task SeedAllDataAsync(UCarDbContext context, bool force = false)
    {
        Console.WriteLine("=== UCar Data Seeder ===");

        // Check if data already exists
        if (await context.Roles.AnyAsync() && !force)
        {
            Console.WriteLine("Database already seeded. Use force=true to reseed.");
            return;
        }

        if (force)
        {
            Console.WriteLine("Force mode: Clearing existing data...");
            await ClearAllDataAsync(context);
        }

        // Seed in dependency order
        var roles = await SeedRolesAsync(context);
        var (users, branches) = await SeedUsersAndBranchesAsync(context, roles);
        var (staffList, customers) = await SeedStaffAndCustomersAsync(context, users, branches, roles);
        var (vehicleTypes, vehicleModels, vehicles) = await SeedVehiclesAsync(context, branches);
        await SeedPricesAsync(context, vehicleTypes, vehicleModels);
        await SeedHolidaysAsync(context, users.adminUser);
        await SeedDepositPoliciesAsync(context, vehicleTypes, users.adminUser);
        await SeedSurchargePoliciesAsync(context, vehicleTypes, users.adminUser);
        await SeedContractsAndHandoversAsync(context, customers, vehicles, vehicleTypes, users.staffUser);
        await SeedShiftsAndTasksAsync(context, staffList, branches, vehicles);

        Console.WriteLine("=== UCar Data Seeder Complete ===");
    }

    private static async Task ClearAllDataAsync(UCarDbContext context)
    {
        // Clear in reverse dependency order - most dependent tables first

        // Incident related
        context.IncidentDocuments.RemoveRange(context.IncidentDocuments);
        context.IncidentCosts.RemoveRange(context.IncidentCosts);
        context.IncidentImpoundDetails.RemoveRange(context.IncidentImpoundDetails);
        context.IncidentFineDetails.RemoveRange(context.IncidentFineDetails);
        context.Incidents.RemoveRange(context.Incidents);

        // Operations related (Module 8.0)
        context.ShiftAssignments.RemoveRange(context.ShiftAssignments);
        context.Shifts.RemoveRange(context.Shifts);
        context.OperationalTasks.RemoveRange(context.OperationalTasks);

        // Contract related - child tables first
        context.HandoverAccessories.RemoveRange(context.HandoverAccessories);
        context.HandoverRecords.RemoveRange(context.HandoverRecords);
        context.ReturnRecords.RemoveRange(context.ReturnRecords);
        context.ContractViolations.RemoveRange(context.ContractViolations);
        context.ContractCharges.RemoveRange(context.ContractCharges);
        context.CollateralItems.RemoveRange(context.CollateralItems);
        context.PaymentTransactions.RemoveRange(context.PaymentTransactions);
        context.RentalContracts.RemoveRange(context.RentalContracts);
        context.Bookings.RemoveRange(context.Bookings);

        // Vehicle related
        context.MaintenanceOrders.RemoveRange(context.MaintenanceOrders);
        context.VehicleStatusHistories.RemoveRange(context.VehicleStatusHistories);
        context.Vehicles.RemoveRange(context.Vehicles);
        context.Prices.RemoveRange(context.Prices);
        context.VehicleModels.RemoveRange(context.VehicleModels);
        context.VehicleTypes.RemoveRange(context.VehicleTypes);

        // Pricing & Policy related (Module 3.0)
        context.HolidayConfigs.RemoveRange(context.HolidayConfigs);
        context.DepositPolicies.RemoveRange(context.DepositPolicies);
        context.SurchargePolicies.RemoveRange(context.SurchargePolicies);

        // User related
        context.CustomerDocuments.RemoveRange(context.CustomerDocuments);
        context.Customers.RemoveRange(context.Customers);
        context.StaffProfiles.RemoveRange(context.StaffProfiles);
        context.UserAccounts.RemoveRange(context.UserAccounts);
        context.Branches.RemoveRange(context.Branches);
        context.Roles.RemoveRange(context.Roles);

        await context.SaveChangesAsync();
        Console.WriteLine("  ✓ Cleared all existing data");
    }

    #region 1. Roles
    private static async Task<(Role admin, Role branchManager, Role staff, Role customer)> SeedRolesAsync(UCarDbContext context)
    {
        Console.WriteLine("Seeding Roles...");

        var adminRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Admin,
            Name = "Quản trị viên",
            IsActive = true
        };

        var branchManagerRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.BranchManager,
            Name = "Quản lý chi nhánh",
            IsActive = true
        };

        var staffRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Staff,
            Name = "Nhân viên",
            IsActive = true
        };

        var customerRole = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = RoleCode.Customer,
            Name = "Khách hàng",
            IsActive = true
        };

        await context.Roles.AddRangeAsync(adminRole, branchManagerRole, staffRole, customerRole);
        await context.SaveChangesAsync();

        Console.WriteLine("  ✓ Created 4 roles (Admin, BranchManager, Staff, Customer)");
        return (adminRole, branchManagerRole, staffRole, customerRole);
    }
    #endregion

    #region 2. Users & Branches
    private static async Task<((UserAccount adminUser, UserAccount staffUser, List<UserAccount> branchManagerUsers, List<UserAccount> customerUsers) users, List<Branch> branches)>
        SeedUsersAndBranchesAsync(UCarDbContext context, (Role admin, Role branchManager, Role staff, Role customer) roles)
    {
        Console.WriteLine("Seeding Users and Branches...");

        string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        // Admin User
        var adminUser = new UserAccount
        {
            UserId = Guid.NewGuid(),
            RoleId = roles.admin.RoleId,
            Username = "admin",
            Email = "admin@ucar.com",
            Phone = "0901234567",
            PasswordHash = HashPassword("admin123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        // Staff User
        var staffUser = new UserAccount
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            RoleId = roles.staff.RoleId,
            Username = "staff01",
            Email = "staff1@ucar.com",
            Phone = "0902345678",
            PasswordHash = HashPassword("staff123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Customer Users
        var customerNames = new[]
        {
            "Lê Hoàng Minh", "Phạm Thị Lan", "Nguyễn Đức Anh",
            "Trần Văn Hùng", "Võ Thị Mai", "Đặng Quốc Bảo"
        };

        var customerUsers = new List<UserAccount>();
        for (int i = 0; i < customerNames.Length; i++)
        {
            customerUsers.Add(new UserAccount
            {
                UserId = Guid.NewGuid(),
                RoleId = roles.customer.RoleId,
                Username = $"customer{(i < 8 ? "0" : "")}{i + 1}",
                Email = $"customer{(i < 8 ? "0" : "")}{i + 1}@gmail.com",
                Phone = $"09{i + 1:D8}",
                PasswordHash = HashPassword("customer123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30 + i)
            });
        }

        await context.UserAccounts.AddAsync(adminUser);
        await context.UserAccounts.AddAsync(staffUser);
        await context.UserAccounts.AddRangeAsync(customerUsers);

        // Branches
        var branches = new List<Branch>
        {
            new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Quận 1", Address = "123 Nguyễn Huệ, Quận 1, TP.HCM", PhoneContact = "028-1234-5678" },
            new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Quận 7", Address = "456 Nguyễn Văn Linh, Quận 7, TP.HCM", PhoneContact = "028-2345-6789" },
            new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Thủ Đức", Address = "789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM", PhoneContact = "028-3456-7890" },
            new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Tân Bình", Address = "321 Cộng Hòa, Quận Tân Bình, TP.HCM", PhoneContact = "028-4567-8901" },
            new() { BranchId = Guid.NewGuid(), Name = "Chi nhánh Bình Thạnh", Address = "654 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM", PhoneContact = "028-5678-9012" }
        };

        await context.Branches.AddRangeAsync(branches);

        // Branch Manager Users (one for each branch)
        var branchManagerUsers = new List<UserAccount>();
        var managerNames = new[] { "Nguyễn Quản Lý 1", "Trần Quản Lý 2", "Lê Quản Lý 3", "Phạm Quản Lý 4", "Hoàng Quản Lý 5" };
        for (int i = 0; i < branches.Count; i++)
        {
            branchManagerUsers.Add(new UserAccount
            {
                UserId = Guid.NewGuid(),
                RoleId = roles.branchManager.RoleId,
                Username = $"manager{i + 1}",
                Email = $"manager{i + 1}@ucar.com",
                Phone = $"091{i + 1:D7}",
                PasswordHash = HashPassword("manager123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.UserAccounts.AddRangeAsync(branchManagerUsers);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {2 + branchManagerUsers.Count + customerUsers.Count} users, {branches.Count} branches");
        return ((adminUser, staffUser, branchManagerUsers, customerUsers), branches);
    }
    #endregion

    #region 3. Staff & Customers
    private static async Task<(List<StaffProfile> staffList, List<Customer> customers)> SeedStaffAndCustomersAsync(
        UCarDbContext context,
        (UserAccount adminUser, UserAccount staffUser, List<UserAccount> branchManagerUsers, List<UserAccount> customerUsers) users,
        List<Branch> branches,
        (Role admin, Role branchManager, Role staff, Role customer) roles)
    {
        Console.WriteLine("Seeding Staff Profiles and Customers...");

        string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        var staffList = new List<StaffProfile>();

        // Create StaffProfile for each BranchManager (linked to their branch)
        for (int i = 0; i < users.branchManagerUsers.Count && i < branches.Count; i++)
        {
            var managerProfile = new StaffProfile
            {
                StaffId = Guid.NewGuid(),
                UserId = users.branchManagerUsers[i].UserId,
                BranchId = branches[i].BranchId,
                StaffCode = $"QL00{i + 1}",
                FullName = $"Quản lý {branches[i].Name}",
                Position = "Quản lý chi nhánh",
                IsActive = true
            };
            staffList.Add(managerProfile);
        }

        // Staff 1 - Existing from staffUser
        var staffProfile1 = new StaffProfile
        {
            StaffId = Guid.NewGuid(),
            UserId = users.staffUser.UserId,
            BranchId = branches[0].BranchId,
            StaffCode = "NV001",
            FullName = "Nguyễn Văn An",
            Position = "Nhân viên giao nhận",
            IsActive = true
        };
        staffList.Add(staffProfile1);

        // Additional Staff for Module 8.0
        var staffNames = new[] { "Trần Thị Bình", "Lê Hoàng Cường", "Phạm Văn Dũng", "Hoàng Thị Em" };
        var positions = new[] { "Nhân viên bảo dưỡng", "Nhân viên giao nhận", "Nhân viên cứu hộ", "Nhân viên hành chính" };

        for (int i = 0; i < staffNames.Length; i++)
        {
            // Create user account for new staff
            var staffUser = new UserAccount
            {
                UserId = Guid.NewGuid(),
                RoleId = roles.staff.RoleId,
                Username = $"staff{i + 2}",
                Email = $"staff{i + 2}@ucar.com",
                Phone = $"090{i + 2:D7}",
                PasswordHash = HashPassword("staff123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.UserAccounts.AddAsync(staffUser);

            var staffProfile = new StaffProfile
            {
                StaffId = Guid.NewGuid(),
                UserId = staffUser.UserId,
                BranchId = branches[i % branches.Count].BranchId,
                StaffCode = $"NV00{i + 2}",
                FullName = staffNames[i],
                Position = positions[i],
                IsActive = true
            };
            staffList.Add(staffProfile);
        }

        await context.StaffProfiles.AddRangeAsync(staffList);

        // Customers
        var customerNames = new[]
        {
            "Lê Hoàng Minh", "Phạm Thị Lan", "Nguyễn Đức Anh",
            "Trần Văn Hùng", "Võ Thị Mai", "Đặng Quốc Bảo"
        };

        var customers = new List<Customer>();
        for (int i = 0; i < users.customerUsers.Count; i++)
        {
            customers.Add(new Customer
            {
                CustomerId = Guid.NewGuid(),
                UserId = users.customerUsers[i].UserId,
                FullName = customerNames[i],
                Dob = new DateTime(1985 + i, 1 + (i % 12), 1 + (i % 28)),
                AddressText = $"{100 + i} Đường {i + 1}, Quận {(i % 5) + 1}, TP.HCM",
                RiskLevel = i < 4 ? "Thấp" : "Trung bình",
                IsBlacklisted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30 + i)
            });
        }

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {staffList.Count} staff profiles, {customers.Count} customers");
        return (staffList, customers);
    }
    #endregion

    #region 4. Vehicles
    private static async Task<(List<VehicleType> types, List<VehicleModel> models, List<Vehicle> vehicles)>
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
        var statuses = new[] { VehicleStatus.Available, VehicleStatus.Available, VehicleStatus.Available,
                               VehicleStatus.Renting, VehicleStatus.Maintenance, VehicleStatus.Reserved };
        var random = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            var model = vehicleModels[random.Next(vehicleModels.Count)];
            var branch = branches[random.Next(branches.Count)];
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
    #endregion

    #region 5. Prices
    private static async Task SeedPricesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, List<VehicleModel> vehicleModels)
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
    #endregion

    #region 5.5. Holiday Configuration
    private static async Task SeedHolidaysAsync(UCarDbContext context, UserAccount adminUser)
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
    #endregion

    #region 6. Contracts & Handovers
    private static async Task SeedContractsAndHandoversAsync(
        UCarDbContext context,
        List<Customer> customers,
        List<Vehicle> vehicles,
        List<VehicleType> vehicleTypes,
        UserAccount staffUser)
    {
        Console.WriteLine("Seeding Bookings, Contracts, and Handover Records...");

        var vehicleType = vehicleTypes.First();
        var price = await context.Prices.FirstAsync();
        var availableVehicles = vehicles.Where(v => v.CurrentStatus == VehicleStatus.Available).Take(6).ToList();

        if (availableVehicles.Count < 6)
        {
            Console.WriteLine($"  ⚠ Not enough available vehicles ({availableVehicles.Count}). Need 6. Skipping contracts.");
            return;
        }

        var staffUserId = staffUser.UserId;
        var bookings = new List<Booking>();
        var contracts = new List<RentalContract>();

        // Contracts waiting for check-out (Active status) - 3 contracts
        for (int i = 0; i < 3; i++)
        {
            var bookingId = Guid.NewGuid();
            var contractId = Guid.NewGuid();

            bookings.Add(new Booking
            {
                BookingId = bookingId,
                CustomerId = customers[i].CustomerId,
                VehicleTypeId = vehicleType.VehicleTypeId,
                AssignedVehicleId = availableVehicles[i].VehicleId,
                StartAt = DateTime.UtcNow.AddHours(-2 + i),
                EndAt = DateTime.UtcNow.AddDays(3 + i),
                Status = BookingStatus.Confirmed,
                EstimatedTotal = 800000 * (3 + i),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = staffUserId
            });

            contracts.Add(new RentalContract
            {
                ContractId = contractId,
                BookingId = bookingId,
                CustomerId = customers[i].CustomerId,
                VehicleId = availableVehicles[i].VehicleId,
                PriceId = price.PriceId,
                PlannedStart = DateTime.UtcNow.AddHours(-2 + i),
                PlannedEnd = DateTime.UtcNow.AddDays(3 + i),
                SnapshotUnitPrice = 800000,
                SnapshotDepositAmount = 5000000,
                Status = RentalContractStatus.Active,
                HandledBy = staffUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        // Contracts in progress (waiting for check-in) - 3 contracts
        for (int i = 3; i < 6; i++)
        {
            var bookingId = Guid.NewGuid();
            var contractId = Guid.NewGuid();

            bookings.Add(new Booking
            {
                BookingId = bookingId,
                CustomerId = customers[i].CustomerId,
                VehicleTypeId = vehicleType.VehicleTypeId,
                AssignedVehicleId = availableVehicles[i].VehicleId,
                StartAt = DateTime.UtcNow.AddDays(-3),
                EndAt = DateTime.UtcNow.AddDays(i - 4),
                Status = BookingStatus.Completed,
                EstimatedTotal = 900000 * 3,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = staffUserId
            });

            contracts.Add(new RentalContract
            {
                ContractId = contractId,
                BookingId = bookingId,
                CustomerId = customers[i].CustomerId,
                VehicleId = availableVehicles[i].VehicleId,
                PriceId = price.PriceId,
                PlannedStart = DateTime.UtcNow.AddDays(-3),
                PlannedEnd = DateTime.UtcNow.AddDays(i - 4),
                ActualStart = DateTime.UtcNow.AddDays(-3),
                SnapshotUnitPrice = 900000,
                SnapshotDepositAmount = 5000000,
                Status = RentalContractStatus.InProgress,
                HandledBy = staffUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            });

            // Update vehicle status to Renting
            availableVehicles[i].CurrentStatus = VehicleStatus.Renting;
        }

        await context.Bookings.AddRangeAsync(bookings);
        await context.RentalContracts.AddRangeAsync(contracts);
        await context.SaveChangesAsync();

        // Handover Records for in-progress contracts
        var handoverRecords = new List<HandoverRecord>();
        for (int i = 3; i < 6; i++)
        {
            var contract = contracts[i];
            var vehicle = availableVehicles[i];

            handoverRecords.Add(new HandoverRecord
            {
                HandoverId = Guid.NewGuid(),
                ContractId = contract.ContractId,
                OdoKmOut = vehicle.CurrentOdoKm,
                FuelLevelOut = 100,
                HandedAt = DateTime.UtcNow.AddDays(-3),
                ExteriorCondition = "Tốt, không có trầy xước",
                InteriorCondition = "Sạch sẽ, đầy đủ phụ kiện",
                PreExistingDamages = i == 4 ? "Vết trầy nhẹ cản trước" : null,
                CustomerConfirmed = true,
                CustomerConfirmedAt = DateTime.UtcNow.AddDays(-3),
                HandedOverBy = staffUserId,
                Note = "Giao xe đúng hẹn"
            });
        }

        await context.HandoverRecords.AddRangeAsync(handoverRecords);

        // Accessories for handover records
        var accessories = new List<HandoverAccessory>();
        foreach (var handover in handoverRecords)
        {
            accessories.AddRange(new[]
            {
                new HandoverAccessory
                {
                    AccessoryId = Guid.NewGuid(),
                    HandoverId = handover.HandoverId,
                    AccessoryName = "Chìa khóa xe",
                    Quantity = 2,
                    EstimatedValue = 5000000,
                    IsReturnedOk = true
                },
                new HandoverAccessory
                {
                    AccessoryId = Guid.NewGuid(),
                    HandoverId = handover.HandoverId,
                    AccessoryName = "Giấy đăng kiểm",
                    Quantity = 1,
                    EstimatedValue = 0,
                    IsReturnedOk = true
                },
                new HandoverAccessory
                {
                    AccessoryId = Guid.NewGuid(),
                    HandoverId = handover.HandoverId,
                    AccessoryName = "Bảo hiểm xe",
                    Quantity = 1,
                    EstimatedValue = 0,
                    IsReturnedOk = true
                }
            });
        }

        await context.HandoverAccessories.AddRangeAsync(accessories);
        await context.SaveChangesAsync();

        var activeCount = contracts.Count(c => c.Status == RentalContractStatus.Active);
        var inProgressCount = contracts.Count(c => c.Status == RentalContractStatus.InProgress);
        Console.WriteLine($"  ✓ Created {bookings.Count} bookings, {contracts.Count} contracts");
        Console.WriteLine($"    - {activeCount} contracts waiting for check-out");
        Console.WriteLine($"    - {inProgressCount} contracts waiting for check-in");
    }
    #endregion

    #region 7. Shifts & Operational Tasks (Module 8.0)
    private static async Task SeedShiftsAndTasksAsync(
        UCarDbContext context,
        List<StaffProfile> staffList,
        List<Branch> branches,
        List<Vehicle> vehicles)
    {
        Console.WriteLine("Seeding Shifts, ShiftAssignments, and OperationalTasks (Module 8.0)...");

        // Create Shifts
        var shifts = new List<Shift>
        {
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca sáng",
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(12, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca sáng từ 7h-12h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca chiều",
                StartTime = new TimeOnly(12, 0),
                EndTime = new TimeOnly(17, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca chiều từ 12h-17h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca tối",
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(22, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca tối từ 17h-22h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca toàn thời gian",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                BranchId = branches[1].BranchId,
                Description = "Ca làm việc cả ngày 8h-17h",
                IsActive = true
            }
        };

        await context.Shifts.AddRangeAsync(shifts);
        await context.SaveChangesAsync();

        // Create Shift Assignments for next 7 days
        var shiftAssignments = new List<ShiftAssignment>();
        var random = new Random(456);
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int day = 0; day < 7; day++)
        {
            var workDate = today.AddDays(day);

            // Assign 2-3 staff to each shift
            foreach (var shift in shifts.Take(3))
            {
                var assignedStaff = staffList.OrderBy(_ => random.Next()).Take(2).ToList();
                foreach (var staff in assignedStaff)
                {
                    shiftAssignments.Add(new ShiftAssignment
                    {
                        AssignmentId = Guid.NewGuid(),
                        ShiftId = shift.ShiftId,
                        StaffId = staff.StaffId,
                        WorkDate = workDate,
                        CreatedBy = staffList[0].UserId,
                        CreatedAt = DateTime.UtcNow,
                        Notes = null
                    });
                }
            }
        }

        await context.ShiftAssignments.AddRangeAsync(shiftAssignments);
        await context.SaveChangesAsync();

        // Create Operational Tasks
        var operationalTasks = new List<OperationalTask>();
        var availableVehicles = vehicles.Where(v => v.CurrentStatus == VehicleStatus.Available).Take(10).ToList();
        var taskTypes = new[] { TaskType.Delivery, TaskType.Return, TaskType.Maintenance, TaskType.Rescue, TaskType.Inspection };
        var taskStatuses = new[] { OpTaskStatus.New, OpTaskStatus.Assigned, OpTaskStatus.InProgress, OpTaskStatus.Completed };

        for (int i = 0; i < 15; i++)
        {
            var taskType = taskTypes[i % taskTypes.Length];
            var status = taskStatuses[i % taskStatuses.Length];
            var vehicle = i < availableVehicles.Count ? availableVehicles[i] : null;
            var staff = status != OpTaskStatus.New ? staffList[i % staffList.Count] : null;
            var scheduledAt = DateTime.UtcNow.AddHours(-24 + (i * 4));

            var task = new OperationalTask
            {
                TaskId = Guid.NewGuid(),
                TaskType = taskType,
                Title = taskType switch
                {
                    TaskType.Delivery => $"Giao xe {vehicle?.PlateNo ?? "N/A"} cho khách",
                    TaskType.Return => $"Nhận xe {vehicle?.PlateNo ?? "N/A"} từ khách",
                    TaskType.Maintenance => $"Bảo dưỡng định kỳ xe {vehicle?.PlateNo ?? "N/A"}",
                    TaskType.Rescue => $"Cứu hộ xe {vehicle?.PlateNo ?? "Khẩn cấp"}",
                    TaskType.Inspection => $"Kiểm tra xe {vehicle?.PlateNo ?? "N/A"}",
                    _ => "Nhiệm vụ vận hành"
                },
                VehicleId = vehicle?.VehicleId,
                AssignedToStaffId = staff?.StaffId,
                BranchId = branches[i % branches.Count].BranchId,
                ScheduledAt = scheduledAt,
                EstimatedDurationMinutes = taskType == TaskType.Maintenance ? 120 : 30,
                Location = taskType == TaskType.Rescue ? $"Đường {i + 1}, Quận {(i % 5) + 1}" : null,
                Status = status,
                CompletedAt = status == OpTaskStatus.Completed ? scheduledAt.AddMinutes(25) : null,
                Notes = status == OpTaskStatus.Completed ? "Hoàn thành đúng hẹn" : null,
                CreatedBy = staffList[0].UserId,
                CreatedAt = scheduledAt.AddHours(-2)
            };

            operationalTasks.Add(task);
        }

        await context.OperationalTasks.AddRangeAsync(operationalTasks);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {shifts.Count} shifts, {shiftAssignments.Count} shift assignments");
        Console.WriteLine($"  ✓ Created {operationalTasks.Count} operational tasks");
    }
    #endregion

    #region 8. Deposit Policies
    private static async Task SeedDepositPoliciesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, UserAccount adminUser)
    {
        Console.WriteLine("Seeding Deposit Policies...");

        var depositPolicies = new List<DepositPolicy>();

        foreach (var vehicleType in vehicleTypes)
        {
            // Standard deposit policy for each vehicle type
            depositPolicies.Add(new DepositPolicy
            {
                DepositPolicyId = Guid.NewGuid(),
                PolicyName = $"Chính sách cọc chuẩn - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                
                // Responsibility Deposit (fixed by vehicle type)
                ResponsibilityDepositAmount = vehicleType.TypeName == "Sedan" ? 2_000_000 :
                                             vehicleType.TypeName == "SUV" ? 5_000_000 :
                                             vehicleType.TypeName == "MPV" ? 3_000_000 :
                                             vehicleType.TypeName == "Hatchback" ? 1_500_000 :
                                             vehicleType.TypeName == "Pickup" ? 4_000_000 : 2_000_000,
                
                // Rental Deposit (50% of rental amount)
                RentalDepositCalculationType = DepositCalculationType.Percentage,
                RentalDepositValue = 50, // 50% of rental amount
                RentalDepositMinimum = 2_000_000, // Min 2M VND
                RentalDepositMaximum = vehicleType.TypeName == "Sedan" ? 10_000_000 : 
                                vehicleType.TypeName == "SUV" ? 20_000_000 : 
                                vehicleType.TypeName == "MPV" ? 15_000_000 : 
                                vehicleType.TypeName == "Hatchback" ? 8_000_000 : 
                                vehicleType.TypeName == "Pickup" ? 15_000_000 : 10_000_000,
                FullRefundCondition = "Trả xe đúng hạn, không hư hỏng, không vi phạm giao thông",
                PartialRefundCondition = "Vi phạm nhẹ, trả xe trễ dưới 24h, hư hỏng nhỏ",
                NoRefundCondition = "Vi phạm nghiêm trọng, mất xe, hư hỏng lớn, trả xe trễ quá 3 ngày",
                RefundProcessingDays = 7,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidTo = null,
                IsActive = true,
                Description = $"Chính sách cọc tiêu chuẩn cho loại xe {vehicleType.TypeName}",
                CreatedAt = DateTime.UtcNow
            });

            // Premium deposit policy (lower deposit for loyal customers - future use)
            depositPolicies.Add(new DepositPolicy
            {
                DepositPolicyId = Guid.NewGuid(),
                PolicyName = $"Chính sách cọc ưu đãi - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                
                // Responsibility Deposit (same as standard)
                ResponsibilityDepositAmount = vehicleType.TypeName == "Sedan" ? 2_000_000 :
                                             vehicleType.TypeName == "SUV" ? 5_000_000 :
                                             vehicleType.TypeName == "MPV" ? 3_000_000 :
                                             vehicleType.TypeName == "Hatchback" ? 1_500_000 :
                                             vehicleType.TypeName == "Pickup" ? 4_000_000 : 2_000_000,
                
                // Rental Deposit (30% for loyal customers)
                RentalDepositCalculationType = DepositCalculationType.Percentage,
                RentalDepositValue = 30, // 30% of rental amount for loyal customers
                RentalDepositMinimum = 1_500_000, // Min 1.5M VND
                RentalDepositMaximum = vehicleType.TypeName == "Sedan" ? 8_000_000 : 
                                vehicleType.TypeName == "SUV" ? 15_000_000 : 
                                vehicleType.TypeName == "MPV" ? 12_000_000 : 
                                vehicleType.TypeName == "Hatchback" ? 6_000_000 : 
                                vehicleType.TypeName == "Pickup" ? 12_000_000 : 8_000_000,
                FullRefundCondition = "Trả xe đúng hạn, không hư hỏng, không vi phạm giao thông",
                PartialRefundCondition = "Vi phạm nhẹ, trả xe trễ dưới 12h",
                NoRefundCondition = "Vi phạm nghiêm trọng, mất xe, hư hỏng lớn",
                RefundProcessingDays = 5,
                ValidFrom = DateTime.UtcNow.AddMonths(-3),
                ValidTo = null,
                IsActive = false, // Not active yet - for future use
                Description = $"Chính sách cọc ưu đãi cho khách hàng thân thiết - {vehicleType.TypeName}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.DepositPolicies.AddRangeAsync(depositPolicies);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {depositPolicies.Count} deposit policies ({vehicleTypes.Count} vehicle types × 2 policies)");
    }
    #endregion

    #region 9. Surcharge Policies
    private static async Task SeedSurchargePoliciesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, UserAccount adminUser)
    {
        Console.WriteLine("Seeding Surcharge Policies...");

        var surchargePolicies = new List<SurchargePolicy>();

        foreach (var vehicleType in vehicleTypes)
        {
            // Overtime surcharge - per hour
            surchargePolicies.Add(new SurchargePolicy
            {
                SurchargePolicyId = Guid.NewGuid(),
                PolicyName = $"Phụ phí quá giờ - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                SurchargeType = SurchargeType.Overtime,
                CalculationType = SurchargeCalculationType.FixedAmount,
                Value = vehicleType.TypeName == "Sedan" ? 50_000 : 
                        vehicleType.TypeName == "SUV" ? 100_000 : 
                        vehicleType.TypeName == "MPV" ? 80_000 : 
                        vehicleType.TypeName == "Hatchback" ? 40_000 : 
                        vehicleType.TypeName == "Pickup" ? 80_000 : 50_000,
                AppliesTo = "Rental",
                Unit = "VND/giờ",
                Description = $"Phụ phí áp dụng khi trả xe muộn hơn giờ quy định - {vehicleType.TypeName}",
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidUntil = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUser.UserId
            });

            // Extra kilometer surcharge
            surchargePolicies.Add(new SurchargePolicy
            {
                SurchargePolicyId = Guid.NewGuid(),
                PolicyName = $"Phụ phí vượt quá km - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                SurchargeType = SurchargeType.ExtraKilometer,
                CalculationType = SurchargeCalculationType.FixedAmount,
                Value = vehicleType.TypeName == "Sedan" ? 3_000 : 
                        vehicleType.TypeName == "SUV" ? 5_000 : 
                        vehicleType.TypeName == "MPV" ? 4_000 : 
                        vehicleType.TypeName == "Hatchback" ? 2_500 : 
                        vehicleType.TypeName == "Pickup" ? 4_000 : 3_000,
                AppliesTo = "Rental",
                Unit = "VND/km",
                Description = $"Phụ phí áp dụng khi vượt quá số km cho phép trong hợp đồng - {vehicleType.TypeName}",
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidUntil = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUser.UserId
            });

            // Cleaning surcharge (for dirty vehicle)
            surchargePolicies.Add(new SurchargePolicy
            {
                SurchargePolicyId = Guid.NewGuid(),
                PolicyName = $"Phụ phí vệ sinh - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                SurchargeType = SurchargeType.CleaningFee,
                CalculationType = SurchargeCalculationType.FixedAmount,
                Value = 200_000, // Fixed 200k for all vehicle types
                AppliesTo = "Return",
                Unit = "VND/lần",
                Description = $"Phụ phí vệ sinh khi xe trả lại quá bẩn - {vehicleType.TypeName}",
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidUntil = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUser.UserId
            });

            // Fuel shortage surcharge
            surchargePolicies.Add(new SurchargePolicy
            {
                SurchargePolicyId = Guid.NewGuid(),
                PolicyName = $"Phụ phí nhiên liệu - {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                SurchargeType = SurchargeType.FuelShortage,
                CalculationType = SurchargeCalculationType.FixedAmount,
                Value = vehicleType.TypeName == "Sedan" ? 30_000 : 
                        vehicleType.TypeName == "SUV" ? 50_000 : 
                        vehicleType.TypeName == "MPV" ? 40_000 : 
                        vehicleType.TypeName == "Hatchback" ? 25_000 : 
                        vehicleType.TypeName == "Pickup" ? 45_000 : 30_000,
                AppliesTo = "Return",
                Unit = "VND/lít",
                Description = $"Phụ phí khi trả xe thiếu nhiên liệu so với khi nhận - {vehicleType.TypeName}",
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidUntil = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUser.UserId
            });
        }

        await context.SurchargePolicies.AddRangeAsync(surchargePolicies);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {surchargePolicies.Count} surcharge policies ({vehicleTypes.Count} vehicle types × 4 policies)");
    }
    #endregion
}
