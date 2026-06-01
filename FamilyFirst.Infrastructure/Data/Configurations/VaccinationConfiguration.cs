using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        builder.ConfigureBaseEntity("tblVaccination", "VaccinationId");

        builder.ToTable("tblVaccination", table =>
        {
            table.HasCheckConstraint(
                "CK_tblVaccination_Status",
                "[Status] IN ('Given', 'Due', 'Overdue', 'NotApplicable')");
        });

        builder.Property(v => v.VaccineName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Due");

        builder.HasIndex(v => v.HealthProfileId)
            .HasDatabaseName("IDX_tblVaccination_HealthProfileId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(v => new { v.DueDate, v.Status })
            .HasDatabaseName("IDX_tblVaccination_DueDate_Status")
            .HasFilter("[IsDeleted] = 0 AND [Status] IN ('Due', 'Overdue') AND [DueDate] IS NOT NULL");

        builder.HasOne(v => v.HealthProfile)
            .WithMany(hp => hp.Vaccinations)
            .HasForeignKey(v => v.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Family)
            .WithMany()
            .HasForeignKey(v => v.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.LinkedVaultDocument)
            .WithMany()
            .HasForeignKey(v => v.LinkedVaultDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
