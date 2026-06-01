using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class HealthProfileConfiguration : IEntityTypeConfiguration<HealthProfile>
{
    public void Configure(EntityTypeBuilder<HealthProfile> builder)
    {
        builder.ConfigureBaseEntity("tblHealthProfile", "HealthProfileId");

        builder.ToTable("tblHealthProfile", table =>
        {
            table.HasCheckConstraint(
                "CK_tblHealthProfile_BloodGroup",
                "[BloodGroup] IN ('', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-')");
        });

        builder.Property(p => p.BloodGroup).HasMaxLength(10).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(p => p.KnownAllergiesJson).HasMaxLength(4000);
        builder.Property(p => p.ChronicConditionsJson).HasMaxLength(2000);
        builder.Property(p => p.PrimaryDoctorName).HasMaxLength(200);
        builder.Property(p => p.PrimaryDoctorPhone).HasMaxLength(20);
        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactRelationship).HasMaxLength(100);
        builder.Property(p => p.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(p => p.OrganDonor).IsRequired().HasDefaultValue(false);

        builder.HasIndex(p => p.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UK_tblHealthProfile_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.FamilyId)
            .HasDatabaseName("IDX_tblHealthProfile_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FamilyMember)
            .WithMany()
            .HasForeignKey(p => p.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Prescriptions)
            .WithOne(pr => pr.HealthProfile)
            .HasForeignKey(pr => pr.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Vaccinations)
            .WithOne(v => v.HealthProfile)
            .HasForeignKey(v => v.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.HealthRecords)
            .WithOne(r => r.HealthProfile)
            .HasForeignKey(r => r.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.HeightWeightRecords)
            .WithOne(h => h.HealthProfile)
            .HasForeignKey(h => h.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.EmergencyCardLinks)
            .WithOne(l => l.HealthProfile)
            .HasForeignKey(l => l.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
