using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class SafeZoneConfiguration : IEntityTypeConfiguration<SafeZone>
{
    public void Configure(EntityTypeBuilder<SafeZone> builder)
    {
        builder.ConfigureBaseEntity("tblSafeZone", "SafeZoneId");

        builder.ToTable("tblSafeZone", table =>
        {
            table.HasCheckConstraint("CK_tblSafeZone_ZoneName", "LEN([ZoneName]) BETWEEN 1 AND 40");
            table.HasCheckConstraint("CK_tblSafeZone_RadiusMetres", "[RadiusMetres] BETWEEN 50 AND 500");
            table.HasCheckConstraint(
                "CK_tblSafeZone_ZoneType",
                "[ZoneType] IN ('Home','School','Tuition','RelativesHouse','Workplace','PlaceOfWorship','Other')");
            table.HasCheckConstraint(
                "CK_tblSafeZone_LateAlertTime",
                "[LateAlertEnabled] = 0 OR [LateAlertTime] IS NOT NULL");
        });

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

        builder.HasIndex(z => z.FamilyId)
            .HasDatabaseName("IDX_tblSafeZone_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(z => z.Family)
            .WithMany()
            .HasForeignKey(z => z.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
