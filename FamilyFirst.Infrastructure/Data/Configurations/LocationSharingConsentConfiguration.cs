using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class LocationSharingConsentConfiguration : IEntityTypeConfiguration<LocationSharingConsent>
{
    public void Configure(EntityTypeBuilder<LocationSharingConsent> builder)
    {
        builder.ToTable("LocationSharingConsent");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("ConsentId").ValueGeneratedOnAdd();

        builder.Property(c => c.ConsentGiven).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.SharingEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.CaregiverViewOnly).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.FamilyMemberId)
            .HasDatabaseName("UX_LocationSharingConsent_FamilyMemberId")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.FamilyId)
            .HasDatabaseName("IX_LocationSharingConsent_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .HasPrincipalKey("Id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
