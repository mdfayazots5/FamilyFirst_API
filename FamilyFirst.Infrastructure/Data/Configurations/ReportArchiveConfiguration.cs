using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class WeeklyDigestArchiveConfiguration : IEntityTypeConfiguration<WeeklyDigestArchive>
{
    public void Configure(EntityTypeBuilder<WeeklyDigestArchive> builder)
    {
        builder.ToTable("WeeklyDigestArchive");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("WeeklyDigestArchiveId").ValueGeneratedOnAdd();

        builder.Property(a => a.DigestContentJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        builder.Property(a => a.ShareableImageUrl).HasMaxLength(1000);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.FamilyId, a.WeekStartDate })
            .HasDatabaseName("UX_WeeklyDigestArchive_FamilyId_WeekStartDate")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

public sealed class ChildPillarScoreHistoryConfiguration : IEntityTypeConfiguration<ChildPillarScoreHistory>
{
    public void Configure(EntityTypeBuilder<ChildPillarScoreHistory> builder)
    {
        builder.ToTable("ChildPillarScoreHistory");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("ChildPillarScoreHistoryId").ValueGeneratedOnAdd();

        builder.Property(h => h.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(h => new { h.ChildProfileId, h.SnapshotMonth })
            .HasDatabaseName("UX_ChildPillarScoreHistory_ChildProfileId_SnapshotMonth")
            .IsUnique();

        builder.HasIndex(h => new { h.FamilyId, h.SnapshotMonth })
            .HasDatabaseName("IX_ChildPillarScoreHistory_FamilyId_SnapshotMonth");

        builder.HasOne(h => h.ChildProfile)
            .WithMany()
            .HasForeignKey(h => h.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Family)
            .WithMany()
            .HasForeignKey(h => h.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // No query filter — append-only table, hard-purged by WeeklyDigestWorker
    }
}
