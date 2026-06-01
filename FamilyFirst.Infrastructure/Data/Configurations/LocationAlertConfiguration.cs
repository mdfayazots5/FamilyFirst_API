using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class LocationAlertConfiguration : IEntityTypeConfiguration<LocationAlert>
{
    public void Configure(EntityTypeBuilder<LocationAlert> builder)
    {
        builder.ConfigureBaseEntity("tblLocationAlert", "LocationAlertId");

        builder.ToTable("tblLocationAlert", table =>
        {
            table.HasCheckConstraint(
                "CK_tblLocationAlert_AlertType",
                "[AlertType] IN ('ZoneArrival','ZoneDeparture','LateAlert','SOS','BatteryWarning','LocationStale','LocationSharingPaused')");
        });

        builder.Property(a => a.AlertType).HasMaxLength(30).IsRequired();
        builder.Property(a => a.ZoneNameSnapshot).HasMaxLength(40);
        builder.Property(a => a.Latitude).HasPrecision(10, 7);
        builder.Property(a => a.Longitude).HasPrecision(10, 7);
        builder.Property(a => a.ResolutionNote).HasMaxLength(500);
        builder.Property(a => a.IsResolved).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.TriggeredAt).IsRequired();

        builder.HasIndex(a => new { a.FamilyId, a.TriggeredAt })
            .HasDatabaseName("IDX_tblLocationAlert_FamilyId_TriggeredAt")
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(a => new { a.FamilyMemberId, a.AlertType })
            .HasDatabaseName("IDX_tblLocationAlert_FamilyMemberId_AlertType")
            .HasFilter("[IsDeleted] = 0 AND [IsResolved] = 0");

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.FamilyMember)
            .WithMany()
            .HasForeignKey(a => a.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SafeZone)
            .WithMany()
            .HasForeignKey(a => a.SafeZoneId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ResolvedByUser)
            .WithMany()
            .HasForeignKey(a => a.ResolvedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
