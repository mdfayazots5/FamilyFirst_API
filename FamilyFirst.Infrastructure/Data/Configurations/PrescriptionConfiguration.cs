using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable(
            "Prescriptions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Prescriptions_EndDate",
                    "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("PrescriptionId").ValueGeneratedOnAdd();

        builder.Property(p => p.MedicationName).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Dosage).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PrescribingDoctor).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IsRecurring).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.IsArchived).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => new { p.HealthProfileId, p.IsArchived })
            .HasDatabaseName("IX_Prescriptions_HealthProfileId_IsArchived")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => new { p.EndDate, p.IsArchived })
            .HasDatabaseName("IX_Prescriptions_EndDate_IsArchived")
            .HasFilter("[IsDeleted] = 0 AND [IsArchived] = 0 AND [EndDate] IS NOT NULL");

        builder.HasOne(p => p.HealthProfile)
            .WithMany(hp => hp.Prescriptions)
            .HasForeignKey(p => p.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LinkedDocument)
            .WithMany()
            .HasForeignKey(p => p.LinkedDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
