using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(auditLog => auditLog.AuditId);

        builder.Property(auditLog => auditLog.AuditId).ValueGeneratedOnAdd();
        builder.Property(auditLog => auditLog.Action).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.IpAddress).HasMaxLength(45);
        builder.Property(auditLog => auditLog.UserAgent).HasMaxLength(500);
        builder.Property(auditLog => auditLog.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(auditLog => new { auditLog.FamilyId, auditLog.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_FamilyId_CreatedAt");

        builder.HasIndex(auditLog => auditLog.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasOne(auditLog => auditLog.User)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(auditLog => auditLog.Family)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
