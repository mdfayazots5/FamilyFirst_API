using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class LocationSharingConsentConfiguration : IEntityTypeConfiguration<LocationSharingConsent>
{
    public void Configure(EntityTypeBuilder<LocationSharingConsent> builder)
    {
        builder.ConfigureBaseEntity("tblLocationSharingConsent", "LocationSharingConsentId");

        builder.Property(c => c.ConsentGiven).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.SharingEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.CaregiverViewOnly).IsRequired().HasDefaultValue(false);

        builder.HasIndex(c => c.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UK_tblLocationSharingConsent_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.FamilyId)
            .HasDatabaseName("IDX_tblLocationSharingConsent_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
