using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class LocationAlertConfiguration : IEntityTypeConfiguration<LocationAlert>
{
    public void Configure(EntityTypeBuilder<LocationAlert> builder)
    {
        builder.ToTable(
            "LocationAlerts",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_LocationAlerts_AlertType",
                    "[AlertType] IN ('ZoneArrival','ZoneDeparture','LateAlert','SOS','BatteryWarning','LocationStale','LocationSharingPaused')");
            });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("LocationAlertId").ValueGeneratedOnAdd();

        builder.Property(a => a.AlertType).HasMaxLength(30).IsRequired();
        builder.Property(a => a.ZoneNameSnapshot).HasMaxLength(40);
        builder.Property(a => a.Latitude).HasPrecision(10, 7);
        builder.Property(a => a.Longitude).HasPrecision(10, 7);
        builder.Property(a => a.ResolutionNote).HasMaxLength(500);
        builder.Property(a => a.IsResolved).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.TriggeredAt).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.FamilyId, a.TriggeredAt })
            .HasDatabaseName("IX_LocationAlerts_FamilyId_TriggeredAt")
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(a => new { a.FamilyMemberId, a.AlertType })
            .HasDatabaseName("IX_LocationAlerts_FamilyMemberId_AlertType")
            .HasFilter("[IsDeleted] = 0 AND [IsResolved] = 0");

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.FamilyMember)
            .WithMany()
            .HasForeignKey(a => a.FamilyMemberId)
            .HasPrincipalKey("Id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Zone)
            .WithMany()
            .HasForeignKey(a => a.ZoneId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ResolvedByUser)
            .WithMany()
            .HasForeignKey(a => a.ResolvedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
