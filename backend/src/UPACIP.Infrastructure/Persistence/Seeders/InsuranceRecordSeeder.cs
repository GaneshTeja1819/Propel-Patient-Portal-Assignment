using Microsoft.EntityFrameworkCore;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds read-only insurance provider reference data using EF Core HasData.
/// Anonymous objects bypass the BaseEntity.Id private setter — EF Core reads
/// property values by name from the objects at migration-generation time and
/// emits them as SQL INSERT statements. Fixed GUIDs ensure idempotency (AC-004).
/// </summary>
internal static class InsuranceRecordSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsuranceRecord>().HasData(
            new
            {
                Id = new Guid("11111111-0000-0000-0000-000000000001"),
                ProviderName = "Blue Cross Blue Shield",
                InsuranceIdPattern = @"^BCBS\d{9}$",
                PlanName = (string?)"PPO Preferred",
                ContactPhone = (string?)"1-800-262-2583",
                IsActive = true
            },
            new
            {
                Id = new Guid("11111111-0000-0000-0000-000000000002"),
                ProviderName = "Aetna",
                InsuranceIdPattern = @"^AET\d{10}$",
                PlanName = (string?)"Aetna Choice POS II",
                ContactPhone = (string?)"1-800-872-3862",
                IsActive = true
            },
            new
            {
                Id = new Guid("11111111-0000-0000-0000-000000000003"),
                ProviderName = "United Healthcare",
                InsuranceIdPattern = @"^UHC[A-Z]\d{8}$",
                PlanName = (string?)"UnitedHealthcare Choice Plus",
                ContactPhone = (string?)"1-866-844-4864",
                IsActive = true
            }
        );
    }
}
