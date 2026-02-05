using Microsoft.EntityFrameworkCore;
using UCar.Data.Seeders;
using UCar.Models;

namespace UCar.Data;

/// <summary>
/// Main Data Seeder Orchestrator for UCar
/// Coordinates all seeder modules and manages data clearing
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

        // Seed in dependency order using helper seeders
        var roles = await RoleAndUserSeeder.SeedRolesAsync(context);
        var (users, branches) = await RoleAndUserSeeder.SeedUsersAndBranchesAsync(context, roles);
        var (staffList, customers) = await StaffAndCustomerSeeder.SeedStaffAndCustomersAsync(context, users, branches, roles);
        await CustomerDocumentSeeder.SeedCustomerDocumentsAsync(context, customers);
        var (vehicleTypes, vehicleModels, vehicles) = await VehicleCatalogSeeder.SeedVehiclesAsync(context, branches);
        await PricingSeeder.SeedPricesAsync(context, vehicleTypes, vehicleModels);
        await PricingSeeder.SeedHolidaysAsync(context, users.adminUser);
        await PolicySeeder.SeedDepositPoliciesAsync(context, vehicleTypes, users.adminUser);
        await PolicySeeder.SeedSurchargePoliciesAsync(context, vehicleTypes, users.adminUser);
        await ContractAndHandoverSeeder.SeedContractsAndHandoversAsync(context, customers, vehicles, vehicleTypes, users.staffUser);
        await OperationsSeeder.SeedShiftsAndTasksAsync(context, staffList, branches, vehicles);

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
}
