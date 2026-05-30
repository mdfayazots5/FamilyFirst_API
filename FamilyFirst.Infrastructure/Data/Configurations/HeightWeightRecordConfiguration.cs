using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class HeightWeightRecordConfiguration : IEntityTypeConfiguration<HeightWeightRecord>
{
    public void Configure(EntityTypeBuilder<HeightWeightRecord> builder)
    {
        builder.ToTable(
            "HeightWeightRecords",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_HeightWeightRecords_HeightOrWeight",
                    "[HeightCm] IS NOT NULL OR [WeightKg] IS NOT NULL");
            });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("HeightWeightRecordId").ValueGeneratedOnAdd();

        builder.Property(h => h.HeightCm).HasPrecision(5, 1);
        builder.Property(h => h.WeightKg).HasPrecision(5, 2);
        builder.Property(h => h.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(h => h.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(h => new { h.HealthProfileId, h.RecordedDate })
            .HasDatabaseName("IX_HeightWeightRecords_HealthProfileId_RecordedDate")
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(h => h.HealthProfile)
            .WithMany(hp => hp.HeightWeightRecords)
            .HasForeignKey(h => h.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Family)
            .WithMany()
            .HasForeignKey(h => h.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.RecordedByUser)
            .WithMany()
            .HasForeignKey(h => h.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}
