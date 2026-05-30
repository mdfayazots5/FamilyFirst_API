using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultExpiryReminderLogConfiguration : IEntityTypeConfiguration<VaultExpiryReminderLog>
{
    public void Configure(EntityTypeBuilder<VaultExpiryReminderLog> builder)
    {
        builder.ToTable("VaultExpiryReminderLogs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ReminderLogId").ValueGeneratedOnAdd();

        builder.Property(r => r.ThresholdDays).IsRequired();
        builder.Property(r => r.SentAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => new { r.DocumentId, r.ThresholdDays })
            .HasDatabaseName("IX_VaultExpiryReminderLogs_DocumentId_ThresholdDays")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.Document)
            .WithMany()
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
