using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class SafeZoneConfiguration : IEntityTypeConfiguration<SafeZone>
{
    public void Configure(EntityTypeBuilder<SafeZone> builder)
    {
        builder.ToTable(
            "SafeZones",
            table =>
            {
                table.HasCheckConstraint("CK_SafeZones_ZoneName",     "LEN([ZoneName]) BETWEEN 1 AND 40");
                table.HasCheckConstraint("CK_SafeZones_RadiusMetres", "[RadiusMetres] BETWEEN 50 AND 500");
                table.HasCheckConstraint(
                    "CK_SafeZones_ZoneType",
                    "[ZoneType] IN ('Home','School','Tuition','RelativesHouse','Workplace','PlaceOfWorship','Other')");
                table.HasCheckConstraint(
                    "CK_SafeZones_LateAlertTime",
                    "[LateAlertEnabled] = 0 OR [LateAlertTime] IS NOT NULL");
            });

        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).HasColumnName("SafeZoneId").ValueGeneratedOnAdd();

        builder.Property(z => z.ZoneName).HasMaxLength(40).IsRequired();
        builder.Property(z => z.ZoneType).HasMaxLength(30).IsRequired();
        builder.Property(z => z.CenterLatitude).HasPrecision(10, 7).IsRequired();
        builder.Property(z => z.CenterLongitude).HasPrecision(10, 7).IsRequired();
        builder.Property(z => z.RadiusMetres).IsRequired().HasDefaultValue(150);
        builder.Property(z => z.AlertOnArrival).IsRequired().HasDefaultValue(true);
        builder.Property(z => z.AlertOnDeparture).IsRequired().HasDefaultValue(true);
        builder.Property(z => z.LateAlertEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(z => z.OverrideQuietHours).IsRequired().HasDefaultValue(true);
        builder.Property(z => z.AppliedMemberIdsJson).HasMaxLength(2000).IsRequired();
        builder.Property(z => z.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(z => z.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(z => new { z.FamilyId, z.IsDeleted })
            .HasDatabaseName("IX_SafeZones_FamilyId_IsDeleted")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(z => z.Family)
            .WithMany()
            .HasForeignKey(z => z.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(z => !z.IsDeleted);
    }
}
