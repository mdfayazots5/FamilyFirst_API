using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class HealthProfileConfiguration : IEntityTypeConfiguration<HealthProfile>
{
    public void Configure(EntityTypeBuilder<HealthProfile> builder)
    {
        builder.ToTable(
            "HealthProfiles",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_HealthProfiles_BloodGroup",
                    "[BloodGroup] IN ('', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-')");
            });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("HealthProfileId").ValueGeneratedOnAdd();

        builder.Property(p => p.BloodGroup).HasMaxLength(10).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(p => p.KnownAllergiesJson).HasMaxLength(4000);
        builder.Property(p => p.ChronicConditionsJson).HasMaxLength(2000);
        builder.Property(p => p.PrimaryDoctorName).HasMaxLength(200);
        builder.Property(p => p.PrimaryDoctorPhone).HasMaxLength(20);
        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactRelationship).HasMaxLength(100);
        builder.Property(p => p.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(p => p.OrganDonor).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => p.FamilyMemberId)
            .HasDatabaseName("UX_HealthProfiles_FamilyMemberId")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => new { p.FamilyId, p.IsDeleted })
            .HasDatabaseName("IX_HealthProfiles_FamilyId_IsDeleted")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FamilyMember)
            .WithMany()
            .HasForeignKey(p => p.FamilyMemberId)
            .HasPrincipalKey("Id")
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

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
