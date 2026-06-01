using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaultExpiryReminderLogConfiguration : IEntityTypeConfiguration<VaultExpiryReminderLog>
{
    public void Configure(EntityTypeBuilder<VaultExpiryReminderLog> builder)
    {
        builder.ConfigureBaseEntity("tblVaultExpiryReminderLog", "VaultExpiryReminderLogId");

        builder.Property(r => r.ThresholdDays).IsRequired();
        builder.Property(r => r.SentAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(r => new { r.VaultDocumentId, r.ThresholdDays })
            .IsUnique()
            .HasDatabaseName("IDX_tblVaultExpiryReminderLog_VaultDocumentId_ThresholdDays")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.VaultDocument)
            .WithMany()
            .HasForeignKey(r => r.VaultDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
