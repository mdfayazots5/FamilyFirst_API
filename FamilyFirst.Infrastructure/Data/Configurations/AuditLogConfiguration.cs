using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ConfigureAppendOnlyEntity("tblAuditLog", "AuditLogId");

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => new { a.FamilyId, a.DateCreated })
            .HasDatabaseName("IDX_tblAuditLog_FamilyId_DateCreated");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IDX_tblAuditLog_UserId");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
