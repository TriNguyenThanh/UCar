using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Customer Documents (CMND/CCCD, GPLX, Passport)
/// Images should be placed in wwwroot/images/{doctype}/ folder
/// </summary>
public static class CustomerDocumentSeeder
{
    public static async Task SeedCustomerDocumentsAsync(UCarDbContext context, List<Customer> customers)
    {
        Console.WriteLine("Seeding Customer Documents...");

        var documents = new List<CustomerDocument>();
        var random = new Random(789);

        foreach (var customer in customers)
        {
            var customerIndex = customers.IndexOf(customer) + 1;

            // 1. ID Card (CMND/CCCD) - All customers must have
            var idNumber = GenerateIdNumber(random);
            documents.Add(new CustomerDocument
            {
                DocId = Guid.NewGuid(),
                CustomerId = customer.CustomerId,
                DocType = CustomerDocumentType.IdCard,
                DocNumber = idNumber,
                IssuedDate = DateTime.UtcNow.AddYears(-random.Next(1, 10)),
                IssuedPlace = GetRandomIssuedPlace(random),
                ImageFrontUrl = $"img/idcard/customer_{customerIndex:D2}_idcard_front.png",
                ImageBackUrl = $"img/idcard/customer_{customerIndex:D2}_idcard_back.png",
                IsVerified = customerIndex <= 4 // First 4 customers verified
            });

            // 2. Driver's License (GPLX) - Most customers have this
            if (customerIndex <= 5) // 5 out of 6 customers have license
            {
                var licenseNumber = GenerateLicenseNumber(random);
                var licenseClass = random.Next(0, 2) == 0 ? "B1" : "B2"; // B1 or B2 license
                
                documents.Add(new CustomerDocument
                {
                    DocId = Guid.NewGuid(),
                    CustomerId = customer.CustomerId,
                    DocType = CustomerDocumentType.License,
                    DocNumber = licenseNumber,
                    IssuedDate = DateTime.UtcNow.AddYears(-random.Next(2, 15)),
                    IssuedPlace = $"Sở GTVT {GetRandomCity(random)} - Hạng {licenseClass}",
                    ImageFrontUrl = $"img/license/customer_{customerIndex:D2}_license_front.png",
                    ImageBackUrl = $"img/license/customer_{customerIndex:D2}_license_back.png",
                    IsVerified = customerIndex <= 3 // First 3 customers verified
                });
            }
        }

        await context.CustomerDocuments.AddRangeAsync(documents);
        await context.SaveChangesAsync();

        var idCardCount = documents.Count(d => d.DocType == CustomerDocumentType.IdCard);
        var licenseCount = documents.Count(d => d.DocType == CustomerDocumentType.License);
        var verifiedCount = documents.Count(d => d.IsVerified);

        Console.WriteLine($"  ✓ Created {documents.Count} customer documents:");
        Console.WriteLine($"    - {idCardCount} ID Cards (CMND/CCCD)");
        Console.WriteLine($"    - {licenseCount} Driver's Licenses (GPLX)");
        Console.WriteLine($"    - {verifiedCount} verified documents");
    }

    private static string GenerateIdNumber(Random random)
    {
        // CMND: 9 digits (old format) or CCCD: 12 digits (new format)
        var isNewFormat = random.Next(0, 2) == 0;
        
        if (isNewFormat)
        {
            // CCCD format: 001 + YY + 2 digits province + 7 digits
            var birthYear = random.Next(85, 100); // 1985-2000
            var province = random.Next(1, 96); // 1-95 provinces
            var sequence = random.Next(1000000, 9999999);
            return $"001{birthYear:D2}{province:D2}{sequence:D7}";
        }
        else
        {
            // CMND format: 9 digits
            var prefix = random.Next(100, 999);
            var suffix = random.Next(100000, 999999);
            return $"{prefix}{suffix}";
        }
    }

    private static string GenerateLicenseNumber(Random random)
    {
        // GPLX format: 12 digits (similar to CCCD)
        var issueYear = random.Next(10, 26); // 2010-2026
        var province = random.Next(1, 96);
        var sequence = random.Next(100000, 999999);
        return $"{issueYear:D2}{province:D2}{sequence:D6}";
    }

    private static string GetRandomIssuedPlace(Random random)
    {
        var places = new[]
        {
            "Công an TP. Hồ Chí Minh",
            "Công an Quận 1, TP.HCM",
            "Công an Quận 7, TP.HCM",
            "Công an Quận Bình Thạnh, TP.HCM",
            "Công an TP. Thủ Đức, TP.HCM",
            "Công an Quận Tân Bình, TP.HCM"
        };
        return places[random.Next(places.Length)];
    }

    private static string GetRandomCity(Random random)
    {
        var cities = new[]
        {
            "TP. Hồ Chí Minh",
            "Hà Nội",
            "Đà Nẵng",
            "Bình Dương",
            "Đồng Nai"
        };
        return cities[random.Next(cities.Length)];
    }
}
