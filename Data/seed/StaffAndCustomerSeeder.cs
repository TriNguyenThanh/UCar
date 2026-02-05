using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Staff Profiles and Customers
/// </summary>
public static class StaffAndCustomerSeeder
{
    public static async Task<(List<StaffProfile> staffList, List<Customer> customers)> SeedStaffAndCustomersAsync(
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
}
