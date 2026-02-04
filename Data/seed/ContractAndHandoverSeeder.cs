using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Bookings, Rental Contracts, and Handover Records
/// </summary>
public static class ContractAndHandoverSeeder
{
    public static async Task SeedContractsAndHandoversAsync(
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

            // Update vehicle status to Reserved for Active contracts
            availableVehicles[i].CurrentStatus = VehicleStatus.Reserved;
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

        // Add VehicleStatusHistory records for status changes
        var statusHistories = new List<VehicleStatusHistory>();
        
        // For Reserved vehicles (indices 0-2)
        for (int i = 0; i < 3; i++)
        {
            statusHistories.Add(new VehicleStatusHistory
            {
                VshId = Guid.NewGuid(),
                VehicleId = availableVehicles[i].VehicleId,
                FromStatus = VehicleStatus.Available.ToString(),
                ToStatus = VehicleStatus.Reserved.ToString(),
                ChangedAt = DateTime.UtcNow.AddDays(-1),
                ChangedBy = staffUserId,
                Note = "Xe được đặt trước - hợp đồng chờ giao xe"
            });
        }
        
        // For Renting vehicles (indices 3-5)
        for (int i = 3; i < 6; i++)
        {
            statusHistories.Add(new VehicleStatusHistory
            {
                VshId = Guid.NewGuid(),
                VehicleId = availableVehicles[i].VehicleId,
                FromStatus = VehicleStatus.Available.ToString(),
                ToStatus = VehicleStatus.Renting.ToString(),
                ChangedAt = DateTime.UtcNow.AddDays(-3),
                ChangedBy = staffUserId,
                Note = "Xe đã giao cho khách hàng - đang trong thời gian thuê"
            });
        }
        
        await context.VehicleStatusHistories.AddRangeAsync(statusHistories);
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

        // Create MaintenanceOrder records for vehicles under maintenance
        var availableForMaintenance = vehicles
            .Where(v => v.CurrentStatus == VehicleStatus.Available)
            .Skip(6) // Skip the 6 vehicles used for contracts
            .Take(4) // Take 4 vehicles for maintenance
            .ToList();

        var maintenanceOrders = new List<MaintenanceOrder>();
        var maintenanceHistories = new List<VehicleStatusHistory>();

        foreach (var vehicle in availableForMaintenance)
        {
            var maintenanceOrder = new MaintenanceOrder
            {
                MoId = Guid.NewGuid(),
                VehicleId = vehicle.VehicleId,
                StartAt = DateTime.UtcNow.AddDays(-2),
                Status = MaintenanceStatus.InProgress,
                Description = "Bảo dưỡng định kỳ: Thay dầu máy, kiểm tra phanh, thay lọc gió",
                TotalCost = 2500000,
                ProviderName = "Garage Thành Công",
                CreatedBy = staffUserId
            };
            
            maintenanceOrders.Add(maintenanceOrder);

            // Update vehicle status to UnderMaintenance
            vehicle.CurrentStatus = VehicleStatus.Maintenance;

            // Add status history
            maintenanceHistories.Add(new VehicleStatusHistory
            {
                VshId = Guid.NewGuid(),
                VehicleId = vehicle.VehicleId,
                FromStatus = VehicleStatus.Available.ToString(),
                ToStatus = VehicleStatus.Maintenance.ToString(),
                ChangedAt = DateTime.UtcNow.AddDays(-2),
                ChangedBy = staffUserId,
                Note = "Xe vào bảo dưỡng định kỳ"
            });
        }

        await context.MaintenanceOrders.AddRangeAsync(maintenanceOrders);
        await context.VehicleStatusHistories.AddRangeAsync(maintenanceHistories);
        await context.SaveChangesAsync();

        var activeCount = contracts.Count(c => c.Status == RentalContractStatus.Active);
        var inProgressCount = contracts.Count(c => c.Status == RentalContractStatus.InProgress);
        Console.WriteLine($"  ✓ Created {bookings.Count} bookings, {contracts.Count} contracts");
        Console.WriteLine($"    - {activeCount} contracts waiting for check-out (Reserved status)");
        Console.WriteLine($"    - {inProgressCount} contracts waiting for check-in (Renting status)");
        Console.WriteLine($"    - {maintenanceOrders.Count} vehicles under maintenance");
    }
}
