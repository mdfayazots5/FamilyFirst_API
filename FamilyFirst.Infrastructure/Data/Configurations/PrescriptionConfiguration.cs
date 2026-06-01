using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ConfigureBaseEntity("tblPrescription", "PrescriptionId");

        builder.ToTable("tblPrescription", table =>
        {
            table.HasCheckConstraint("CK_tblPrescription_EndDate", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
        });

        builder.Property(p => p.MedicationName).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Dosage).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PrescribingDoctor).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IsRecurring).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.IsArchived).IsRequired().HasDefaultValue(false);

        builder.HasIndex(p => new { p.HealthProfileId, p.IsArchived })
            .HasDatabaseName("IDX_tblPrescription_HealthProfileId_IsArchived")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => new { p.EndDate, p.IsArchived })
            .HasDatabaseName("IDX_tblPrescription_EndDate_IsArchived")
            .HasFilter("[IsDeleted] = 0 AND [IsArchived] = 0 AND [EndDate] IS NOT NULL");

        builder.HasOne(p => p.HealthProfile)
            .WithMany(hp => hp.Prescriptions)
            .HasForeignKey(p => p.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LinkedVaultDocument)
            .WithMany()
            .HasForeignKey(p => p.LinkedVaultDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
