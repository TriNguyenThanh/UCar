using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Roles, Users, and Branches (Foundation Layer)
/// </summary>
public static class RoleAndUserSeeder
{
    public static async Task<(Role admin, Role branchManager, Role staff, Role customer)> SeedRolesAsync(UCarDbContext context)
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

    public static async Task<((UserAccount adminUser, UserAccount staffUser, List<UserAccount> branchManagerUsers, List<UserAccount> customerUsers) users, List<Branch> branches)>
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
}
