using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

// LocationHistory is append-only — hard-deleted after 30 days by SafetyWorker (DPDP Act 2023).
public sealed class LocationHistoryConfiguration : IEntityTypeConfiguration<LocationHistory>
{
    public void Configure(EntityTypeBuilder<LocationHistory> builder)
    {
        builder.ConfigureAppendOnlyEntity("tblLocationHistory", "LocationHistoryId");

        builder.ToTable("tblLocationHistory", table =>
        {
            table.HasCheckConstraint("CK_tblLocationHistory_BatteryLevel", "[BatteryLevel] BETWEEN 0 AND 100");
        });

        builder.Property(l => l.Latitude).HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.Longitude).HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.BatteryLevel).IsRequired();
        builder.Property(l => l.LocationName).HasMaxLength(300);
        builder.Property(l => l.RecordedAt).IsRequired();

        builder.HasIndex(l => new { l.FamilyMemberId, l.RecordedAt })
            .HasDatabaseName("IDX_tblLocationHistory_FamilyMemberId_RecordedAt")
            .IsDescending(false, true);

        builder.HasOne(l => l.Family)
            .WithMany()
            .HasForeignKey(l => l.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.FamilyMember)
            .WithMany()
            .HasForeignKey(l => l.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
