using UCar.Data;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data;

public class PricingPolicyDataSeeder
{
    public static async Task SeedAsync(UCarDbContext context)
    {
        // Kiểm tra nếu đã có data thì không seed nữa
        if (context.DepositPolicies.Any() || context.SurchargePolicies.Any())
        {
            return;
        }

        var vehicleTypes = context.VehicleTypes.ToList();

        // ===== SEED DEPOSIT POLICIES =====
        var depositPolicies = new List<DepositPolicy>();

        foreach (var vehicleType in vehicleTypes)
        {
            depositPolicies.Add(new DepositPolicy
            {
                DepositPolicyId = Guid.NewGuid(),
                PolicyName = $"Chính sách đặt cọc {vehicleType.TypeName}",
                VehicleTypeId = vehicleType.VehicleTypeId,
                CalculationType = DepositCalculationType.Percentage,
                Value = 30m, // 30% giá trị hợp đồng
                MinimumAmount = vehicleType.TypeName.Contains("4 chỗ") ? 2000000m : 3000000m,
                MaximumAmount = vehicleType.TypeName.Contains("4 chỗ") ? 10000000m : 15000000m,
                FullRefundCondition = "Trả xe đúng hạn, không vi phạm, xe không hư hỏng",
                PartialRefundCondition = "Trả xe muộn dưới 6 giờ hoặc có hư hỏng nhỏ",
                NoRefundCondition = "Trả xe muộn trên 24 giờ, vi phạm nghiêm trọng, mất xe hoặc hư hỏng nặng",
                RefundProcessingDays = 7,
                ValidFrom = DateTime.Now.AddMonths(-1),
                ValidTo = null,
                IsActive = true,
                Description = $"Chính sách đặt cọc chuẩn cho {vehicleType.TypeName}",
                CreatedAt = DateTime.Now
            });
        }

        // ===== SEED SURCHARGE POLICIES =====
        var surchargePolicies = new List<SurchargePolicy>();

        // 1. Phụ phí quá giờ
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí quá giờ",
            Type = SurchargeType.Overtime,
            VehicleTypeId = null, // Áp dụng cho tất cả
            CalculationType = SurchargeCalculationType.PerUnit,
            Value = 50000m, // 50k/giờ
            MinimumAmount = 50000m,
            MaximumAmount = 500000m,
            ApplicableCondition = "Áp dụng khi trả xe muộn hơn thời gian quy định",
            Priority = 10,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phụ phí 50,000 VNĐ/giờ khi trả xe muộn",
            CreatedAt = DateTime.Now
        });

        // 2. Phụ phí quá km
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí quá km",
            Type = SurchargeType.ExtraKilometer,
            VehicleTypeId = null,
            CalculationType = SurchargeCalculationType.PerUnit,
            Value = 5000m, // 5k/km
            MinimumAmount = null,
            MaximumAmount = null,
            ApplicableCondition = "Áp dụng khi lái xe vượt quá giới hạn km quy định (300km/ngày)",
            Priority = 9,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phụ phí 5,000 VNĐ/km khi vượt quá 300km/ngày",
            CreatedAt = DateTime.Now
        });

        // 3. Phụ phí giao xe tận nơi
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí giao xe tận nơi",
            Type = SurchargeType.DeliveryService,
            VehicleTypeId = null,
            CalculationType = SurchargeCalculationType.FixedAmount,
            Value = 100000m,
            MinimumAmount = null,
            MaximumAmount = null,
            ApplicableCondition = "Áp dụng khi khách hàng yêu cầu giao xe tận nơi",
            Priority = 5,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phí cố định 100,000 VNĐ cho dịch vụ giao xe tận nơi",
            CreatedAt = DateTime.Now
        });

        // 4. Phụ phí ngày lễ/cuối tuần
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí ngày lễ/cuối tuần",
            Type = SurchargeType.HolidayWeekend,
            VehicleTypeId = null,
            CalculationType = SurchargeCalculationType.Percentage,
            Value = 15m, // 15%
            MinimumAmount = 100000m,
            MaximumAmount = 1000000m,
            ApplicableCondition = "Áp dụng cho thuê xe vào thứ 7, CN, lễ Tết",
            Priority = 8,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phụ phí 15% cho ngày lễ và cuối tuần",
            CreatedAt = DateTime.Now
        });

        // 5. Phụ phí vệ sinh xe bẩn
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí vệ sinh xe bẩn",
            Type = SurchargeType.CleaningFee,
            VehicleTypeId = null,
            CalculationType = SurchargeCalculationType.FixedAmount,
            Value = 200000m,
            MinimumAmount = null,
            MaximumAmount = null,
            ApplicableCondition = "Áp dụng khi xe trả về quá bẩn, cần vệ sinh đặc biệt",
            Priority = 6,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phí cố định 200,000 VNĐ khi xe cần vệ sinh đặc biệt",
            CreatedAt = DateTime.Now
        });

        // 6. Phụ phí thuê lái xe
        surchargePolicies.Add(new SurchargePolicy
        {
            SurchargePolicyId = Guid.NewGuid(),
            PolicyName = "Phụ phí thuê lái xe",
            Type = SurchargeType.DriverService,
            VehicleTypeId = null,
            CalculationType = SurchargeCalculationType.PerUnit,
            Value = 300000m, // 300k/ngày
            MinimumAmount = null,
            MaximumAmount = null,
            ApplicableCondition = "Áp dụng khi khách hàng yêu cầu thuê kèm lái xe",
            Priority = 7,
            ValidFrom = DateTime.Now.AddMonths(-1),
            ValidTo = null,
            IsActive = true,
            Description = "Phí 300,000 VNĐ/ngày cho dịch vụ thuê lái xe",
            CreatedAt = DateTime.Now
        });

        await context.DepositPolicies.AddRangeAsync(depositPolicies);
        await context.SurchargePolicies.AddRangeAsync(surchargePolicies);
        await context.SaveChangesAsync();

        Console.WriteLine($"✓ Seeded {depositPolicies.Count} deposit policies");
        Console.WriteLine($"✓ Seeded {surchargePolicies.Count} surcharge policies");
    }
}
