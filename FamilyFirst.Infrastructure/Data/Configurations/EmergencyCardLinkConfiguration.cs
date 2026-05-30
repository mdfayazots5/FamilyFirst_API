using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class EmergencyCardLinkConfiguration : IEntityTypeConfiguration<EmergencyCardLink>
{
    public void Configure(EntityTypeBuilder<EmergencyCardLink> builder)
    {
        builder.ToTable("EmergencyCardLinks");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("EmergencyCardLinkId").ValueGeneratedOnAdd();

        builder.Property(l => l.Token).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Language).HasMaxLength(10).IsRequired().HasDefaultValue("en");
        builder.Property(l => l.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(l => l.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(l => l.Token)
            .HasDatabaseName("UX_EmergencyCardLinks_Token")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(l => new { l.HealthProfileId, l.IsRevoked })
            .HasDatabaseName("IX_EmergencyCardLinks_HealthProfileId_IsRevoked")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(l => l.HealthProfile)
            .WithMany(hp => hp.EmergencyCardLinks)
            .HasForeignKey(l => l.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Family)
            .WithMany()
            .HasForeignKey(l => l.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CreatedByUser)
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
