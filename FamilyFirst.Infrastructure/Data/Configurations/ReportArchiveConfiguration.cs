using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class WeeklyDigestArchiveConfiguration : IEntityTypeConfiguration<WeeklyDigestArchive>
{
    public void Configure(EntityTypeBuilder<WeeklyDigestArchive> builder)
    {
        builder.ConfigureBaseEntity("tblWeeklyDigestArchive", "WeeklyDigestArchiveId");

        builder.Property(a => a.DigestContentJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        builder.Property(a => a.ShareableImageUrl).HasMaxLength(1000);

        builder.HasIndex(a => new { a.FamilyId, a.WeekStartDate })
            .IsUnique()
            .HasDatabaseName("UK_tblWeeklyDigestArchive_FamilyId_WeekStartDate")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Append-only — auto-purged after 13 months by WeeklyDigestWorker.
public sealed class ChildPillarScoreHistoryConfiguration : IEntityTypeConfiguration<ChildPillarScoreHistory>
{
    public void Configure(EntityTypeBuilder<ChildPillarScoreHistory> builder)
    {
        builder.ConfigureAppendOnlyEntity("tblChildPillarScoreHistory", "ChildPillarScoreHistoryId");

        builder.HasIndex(h => new { h.ChildProfileId, h.SnapshotMonth })
            .IsUnique()
            .HasDatabaseName("UK_tblChildPillarScoreHistory_ChildProfileId_SnapshotMonth");

        builder.HasIndex(h => new { h.FamilyId, h.SnapshotMonth })
            .HasDatabaseName("IDX_tblChildPillarScoreHistory_FamilyId_SnapshotMonth");

        builder.HasOne(h => h.ChildProfile)
            .WithMany()
            .HasForeignKey(h => h.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Family)
            .WithMany()
            .HasForeignKey(h => h.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
