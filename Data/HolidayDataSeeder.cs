using Microsoft.EntityFrameworkCore;
using UCar.Models;

namespace UCar.Data;

/// <summary>
/// Seed dữ liệu ngày lễ/ngày nghỉ lớn của Việt Nam
/// </summary>
public static class HolidayDataSeeder
{
    public static void SeedHolidays(ModelBuilder modelBuilder)
    {
        var holidays = new List<HolidayConfig>
        {
            // === NGÀY LỄ DƯƠNG LỊCH CỐ ĐỊNH ===
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Tết Dương lịch",
                Date = new DateTime(DateTime.Now.Year, 1, 1),
                IsRecurring = true,
                Description = "Ngày Tết Dương lịch (01/01) - Ngày đầu năm mới",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Giỗ Tổ Hùng Vương",
                LunarDate = "10/03", // Mùng 10 tháng 3 âm lịch
                IsRecurring = true,
                Description = "Giỗ Tổ Hùng Vương (10/03 Âm lịch) - Thường rơi vào tháng 4 dương lịch",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Ngày Thống nhất đất nước",
                Date = new DateTime(DateTime.Now.Year, 4, 30),
                IsRecurring = true,
                Description = "Ngày Giải phóng miền Nam, thống nhất đất nước (30/04)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Ngày Quốc tế Lao động",
                Date = new DateTime(DateTime.Now.Year, 5, 1),
                IsRecurring = true,
                Description = "Ngày Quốc tế Lao động (01/05)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Ngày Quốc khánh",
                Date = new DateTime(DateTime.Now.Year, 9, 2),
                IsRecurring = true,
                Description = "Ngày Quốc khánh Việt Nam (02/09)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            // === NGÀY LỄ ÂM LỊCH (Lưu dạng LunarDate) ===
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Tết Nguyên Đán - Ngày 1",
                LunarDate = "01/01", // Mùng 1 Tết
                IsRecurring = true,
                Description = "Tết Nguyên Đán (01/01 Âm lịch) - Ngày đầu năm âm lịch",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Tết Nguyên Đán - Ngày 2",
                LunarDate = "02/01", // Mùng 2 Tết
                IsRecurring = true,
                Description = "Tết Nguyên Đán (02/01 Âm lịch)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Name = "Tết Nguyên Đán - Ngày 3",
                LunarDate = "03/01", // Mùng 3 Tết
                IsRecurring = true,
                Description = "Tết Nguyên Đán (03/01 Âm lịch)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Name = "Tết Nguyên Đán - Ngày 4",
                LunarDate = "04/01", // Mùng 4 Tết
                IsRecurring = true,
                Description = "Tết Nguyên Đán (04/01 Âm lịch)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Tết Nguyên Đán - Ngày 5",
                LunarDate = "05/01", // Mùng 5 Tết
                IsRecurring = true,
                Description = "Tết Nguyên Đán (05/01 Âm lịch) - Ngày cuối cùng nghỉ Tết",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            
            // === KỲ NGHỈ DÀI NGÀY (Sử dụng StartDate/EndDate) ===
            
            new HolidayConfig
            {
                HolidayId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Kỳ nghỉ lễ 30/4 - 1/5 (2026)",
                StartDate = new DateTime(2026, 4, 30),
                EndDate = new DateTime(2026, 5, 3),
                IsRecurring = false, // Năm nào cũng khác vì nghỉ bù
                Description = "Kỳ nghỉ lễ 30/4 - 01/05 năm 2026 (bao gồm nghỉ bù)",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        };

        modelBuilder.Entity<HolidayConfig>().HasData(holidays);
    }
}
