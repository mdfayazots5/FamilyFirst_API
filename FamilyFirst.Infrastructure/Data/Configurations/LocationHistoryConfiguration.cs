using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

// LocationHistory does NOT inherit BaseEntity — append-only, hard-deleted after 30 days.
public sealed class LocationHistoryConfiguration : IEntityTypeConfiguration<LocationHistory>
{
    public void Configure(EntityTypeBuilder<LocationHistory> builder)
    {
        builder.ToTable(
            "LocationHistory",
            table =>
            {
                table.HasCheckConstraint("CK_LocationHistory_BatteryLevel", "[BatteryLevel] BETWEEN 0 AND 100");
            });

        builder.HasKey(l => l.LocationHistoryId);
        builder.Property(l => l.LocationHistoryId).HasColumnName("LocationHistoryId").ValueGeneratedOnAdd();

        builder.Property(l => l.Latitude).HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.Longitude).HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.BatteryLevel).IsRequired();
        builder.Property(l => l.LocationName).HasMaxLength(300);
        builder.Property(l => l.RecordedAt).IsRequired();
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(l => new { l.FamilyMemberId, l.RecordedAt })
            .HasDatabaseName("IX_LocationHistory_FamilyMemberId_RecordedAt")
            .IsDescending(false, true);

        builder.HasOne(l => l.Family)
            .WithMany()
            .HasForeignKey(l => l.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.FamilyMember)
            .WithMany()
            .HasForeignKey(l => l.FamilyMemberId)
            .HasPrincipalKey("Id")
            .OnDelete(DeleteBehavior.Restrict);

        // No global query filter — table has no IsDeleted column
    }
}
