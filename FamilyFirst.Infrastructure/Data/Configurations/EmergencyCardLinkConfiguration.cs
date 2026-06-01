using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class EmergencyCardLinkConfiguration : IEntityTypeConfiguration<EmergencyCardLink>
{
    public void Configure(EntityTypeBuilder<EmergencyCardLink> builder)
    {
        builder.ConfigureBaseEntity("tblEmergencyCardLink", "EmergencyCardLinkId");

        builder.Property(l => l.Token).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Language).HasMaxLength(10).IsRequired().HasDefaultValue("en");
        builder.Property(l => l.IsRevoked).IsRequired().HasDefaultValue(false);

        builder.HasIndex(l => l.Token)
            .IsUnique()
            .HasDatabaseName("UK_tblEmergencyCardLink_Token")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(l => new { l.HealthProfileId, l.IsRevoked })
            .HasDatabaseName("IDX_tblEmergencyCardLink_HealthProfileId_IsRevoked")
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
    }
}
