using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

/// <summary>
/// Seed data cho module Giao nhận xe (6.0)
/// Tạo sample: Roles, Users, Customers, Booking, Price, RentalContracts
/// </summary>
public static class HandoverDataSeeder
{
    public static async Task SeedHandoverDataAsync(UCarDbContext context, bool force = false)
    {
        // Check if data already exist - don't reseed to avoid FK conflicts
        if (await context.RentalContracts.AnyAsync())
        {
            Console.WriteLine("HandoverDataSeeder: RentalContracts exist. Skipping seed.");
            return;
        }
        
        if (await context.Customers.AnyAsync())
        {
            Console.WriteLine("HandoverDataSeeder: Customers exist. Skipping seed.");
            return;
        }

        Console.WriteLine("HandoverDataSeeder: Seeding data...");

        // Get existing data
        var vehicles = await context.Vehicles.Include(v => v.Model).ToListAsync();
        var branches = await context.Branches.ToListAsync();

        if (!vehicles.Any() || !branches.Any())
        {
            Console.WriteLine("HandoverDataSeeder: No vehicles or branches found. Run VehicleDataSeeder first.");
            return;
        }

        // 1. Seed Roles (if not exists)
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Admin);
        var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Staff);
        var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Customer);

        if (adminRole == null)
        {
            var roles = new List<Role>
            {
                new() { RoleId = Guid.NewGuid(), Code = RoleCode.Admin, Name = "Quản trị viên", IsActive = true },
                new() { RoleId = Guid.NewGuid(), Code = RoleCode.Staff, Name = "Nhân viên", IsActive = true },
                new() { RoleId = Guid.NewGuid(), Code = RoleCode.Customer, Name = "Khách hàng", IsActive = true }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            adminRole = roles[0];
            staffRole = roles[1];
            customerRole = roles[2];
        }

        // 2. Seed Staff Users (if not exists)
        var staffUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var staffUser = await context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == staffUserId);
        if (staffUser == null)
        {
            staffUser = new UserAccount
            {
                UserId = staffUserId,
                RoleId = staffRole!.RoleId,
                Username = "staff1",
                Email = "staff1@ucar.com",
                Phone = "0901234567",
                PasswordHash = "hashed_password",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.UserAccounts.AddAsync(staffUser);
            await context.SaveChangesAsync();

            // Create StaffProfile
            var branch1 = branches[0];
            var staffProfile = new StaffProfile
            {
                StaffId = Guid.NewGuid(),
                UserId = staffUserId,
                BranchId = branch1.BranchId,
                StaffCode = "NV001",
                FullName = "Nguyễn Văn An",
                Position = "Nhân viên giao nhận",
                IsActive = true
            };
            await context.StaffProfiles.AddAsync(staffProfile);
            await context.SaveChangesAsync();
        }

        // 3. Seed Customer Users and Customers
        var customerNames = new[]
        {
            "Lê Hoàng Minh", "Phạm Thị Lan", "Nguyễn Đức Anh", "Trần Văn Hùng", "Võ Thị Mai",
            "Đặng Quốc Bảo"
        };

        var customerUsers = new List<UserAccount>();
        var customers = new List<Customer>();

        for (int i = 0; i < customerNames.Length; i++)
        {
            var userId = Guid.NewGuid();
            customerUsers.Add(new UserAccount
            {
                UserId = userId,
                RoleId = customerRole!.RoleId,
                Username = $"customer{i + 1}",
                Email = $"customer{i + 1}@gmail.com",
                Phone = $"09{i + 1:D8}",
                PasswordHash = "hashed_password",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30 + i)
            });

            customers.Add(new Customer
            {
                CustomerId = Guid.NewGuid(),
                UserId = userId,
                FullName = customerNames[i],
                Dob = new DateTime(1985 + i, 1 + (i % 12), 1 + (i % 28)),
                AddressText = $"{100 + i} Đường {i + 1}, Quận {(i % 5) + 1}, TP.HCM",
                RiskLevel = i < 4 ? "Thấp" : "Trung bình",
                IsBlacklisted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30 + i)
            });
        }

        await context.UserAccounts.AddRangeAsync(customerUsers);
        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        // 4. Seed VehicleTypes for Bookings (get first one)
        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync();
        if (vehicleType == null)
        {
            Console.WriteLine("HandoverDataSeeder: No VehicleType found. Skipping contracts.");
            return;
        }

        // 5. Seed Price (if not exists)
        var price = await context.Prices.FirstOrDefaultAsync();
        if (price == null)
        {
            price = new Price
            {
                PriceId = Guid.NewGuid(),
                VehicleTypeId = vehicleType.VehicleTypeId,
                Name = "Giá thuê ngày chuẩn",
                Unit = PriceUnit.Day,
                UnitPrice = 800000,
                OvertimeHourlyPrice = 50000,
                DepositSuggest = 5000000,
                ValidFrom = DateTime.UtcNow.AddMonths(-1),
                IsActive = true
            };
            await context.Prices.AddAsync(price);
            await context.SaveChangesAsync();
        }

        // 6. Seed Bookings and Contracts
        var availableVehicles = vehicles.Where(v => v.CurrentStatus == VehicleStatus.Available).Take(6).ToList();
        
        if (availableVehicles.Count < 6)
        {
            Console.WriteLine($"HandoverDataSeeder: Not enough available vehicles ({availableVehicles.Count}). Need 6.");
            return;
        }

        var bookings = new List<Booking>();
        var contracts = new List<RentalContract>();

        // 6a. Contracts waiting for check-out (Active status) - 3 contracts
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

        // 6b. Contracts in progress (waiting for check-in) - 3 contracts
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
                EndAt = DateTime.UtcNow.AddDays(i - 4), // Some are overdue
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

            // Update vehicle status
            availableVehicles[i].CurrentStatus = VehicleStatus.Renting;
        }

        await context.Bookings.AddRangeAsync(bookings);
        await context.RentalContracts.AddRangeAsync(contracts);
        await context.SaveChangesAsync();

        // 7. Create HandoverRecords for in-progress contracts
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

        // 8. Create accessories for handover records
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

        Console.WriteLine($"HandoverDataSeeder: Created {customers.Count} customers, {contracts.Count} contracts.");
        Console.WriteLine($"  - {contracts.Count(c => c.Status == RentalContractStatus.Active)} contracts waiting for check-out");
        Console.WriteLine($"  - {contracts.Count(c => c.Status == RentalContractStatus.InProgress)} contracts waiting for check-in");
    }
}
