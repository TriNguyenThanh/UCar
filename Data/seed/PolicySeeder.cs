using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Deposit and Surcharge Policies (Module 3.0)
/// </summary>
public static class PolicySeeder
{
    public static async Task SeedDepositPoliciesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, UserAccount adminUser)
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
                ResponsibilityDepositAmount = vehicleType.TypeName == "Sedan" ? 3_000_000 : 
                                              vehicleType.TypeName == "SUV" ? 5_000_000 : 
                                              vehicleType.TypeName == "MPV" ? 4_000_000 : 
                                              vehicleType.TypeName == "Hatchback" ? 2_000_000 : 
                                              vehicleType.TypeName == "Pickup" ? 4_000_000 : 3_000_000,
                RentalDepositCalculationType = DepositCalculationType.Percentage,
                RentalDepositValue = 50, // 50% of rental amount
                RentalDepositMinimum = 2_000_000, // Min 2M VND
                RentalDepositMaximum = vehicleType.TypeName == "Sedan" ? 5_000_000 : 
                                       vehicleType.TypeName == "SUV" ? 10_000_000 : 
                                       vehicleType.TypeName == "MPV" ? 8_000_000 : 
                                       vehicleType.TypeName == "Hatchback" ? 3_000_000 : 
                                       vehicleType.TypeName == "Pickup" ? 8_000_000 : 5_000_000,
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
                ResponsibilityDepositAmount = vehicleType.TypeName == "Sedan" ? 2_000_000 : 
                                              vehicleType.TypeName == "SUV" ? 3_500_000 : 
                                              vehicleType.TypeName == "MPV" ? 3_000_000 : 
                                              vehicleType.TypeName == "Hatchback" ? 1_500_000 : 
                                              vehicleType.TypeName == "Pickup" ? 3_000_000 : 2_000_000,
                RentalDepositCalculationType = DepositCalculationType.Percentage,
                RentalDepositValue = 30, // 30% of rental amount for loyal customers
                RentalDepositMinimum = 1_500_000, // Min 1.5M VND
                RentalDepositMaximum = vehicleType.TypeName == "Sedan" ? 3_000_000 : 
                                       vehicleType.TypeName == "SUV" ? 7_000_000 : 
                                       vehicleType.TypeName == "MPV" ? 5_000_000 : 
                                       vehicleType.TypeName == "Hatchback" ? 2_000_000 : 
                                       vehicleType.TypeName == "Pickup" ? 5_000_000 : 3_000_000,
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

    public static async Task SeedSurchargePoliciesAsync(UCarDbContext context, List<VehicleType> vehicleTypes, UserAccount adminUser)
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
}
