using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultFamilySettingsConfiguration : IEntityTypeConfiguration<VaultFamilySettings>
{
    public void Configure(EntityTypeBuilder<VaultFamilySettings> builder)
    {
        builder.ConfigureBaseEntity("tblVaultFamilySettings", "VaultFamilySettingsId");

        builder.ToTable("tblVaultFamilySettings", table =>
        {
            table.HasCheckConstraint(
                "CK_tblVaultFamilySettings_EmergencyAccessMode",
                "[EmergencyAccessMode] BETWEEN 1 AND 3");
        });

        builder.Property(s => s.EmergencyAccessMode).HasConversion<int>().IsRequired().HasDefaultValue(EmergencyAccessMode.LoginRequired);
        builder.Property(s => s.EmergencyPinHash).HasMaxLength(200);

        // Level 2 Admin Config (script 066)
        builder.Property(s => s.StorageMode).HasMaxLength(20).IsRequired().HasDefaultValue("AppManaged");
        builder.Property(s => s.StorageQuotaAlertThresholdPct).IsRequired().HasDefaultValue(90);
        builder.Property(s => s.OfflineCacheSizeMb).IsRequired().HasDefaultValue(500);
        builder.Property(s => s.HybridRoutingJson).HasMaxLength(4000);
        builder.Property(s => s.EmergencyLinkExpiryHours).IsRequired().HasDefaultValue(72);
        builder.Property(s => s.EmergencyContactsJson).HasMaxLength(1000);
        builder.Property(s => s.FinanceLargeTransactionThreshold).HasPrecision(18, 2).IsRequired().HasDefaultValue(5000m);
        builder.Property(s => s.DocExpiryLeadDaysDefault).IsRequired().HasDefaultValue(30);
        builder.Property(s => s.DocExpiryLeadDaysIdentity).IsRequired().HasDefaultValue(60);
        builder.Property(s => s.DocExpiryLeadDaysMedical).IsRequired().HasDefaultValue(30);
        builder.Property(s => s.DocExpiryLeadDaysInsurance).IsRequired().HasDefaultValue(45);
        builder.Property(s => s.LateArrivalToleranceMinutes).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.LocationStaleThresholdMinutes).IsRequired().HasDefaultValue(60);
        builder.Property(s => s.DefaultAdultEarningMemberTier).IsRequired().HasDefaultValue(2);
        builder.Property(s => s.DefaultIndependentMemberTier).IsRequired().HasDefaultValue(3);
        builder.Property(s => s.ConsentReminderIntervalDays).IsRequired().HasDefaultValue(30);
        builder.Property(s => s.AutoExcludeSalaryCredits).IsRequired().HasDefaultValue(true);

        builder.HasIndex(s => s.FamilyId)
            .IsUnique()
            .HasDatabaseName("UK_tblVaultFamilySettings_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
