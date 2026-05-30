using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        builder.ToTable(
            "Vaccinations",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Vaccinations_Status",
                    "[Status] IN ('Given', 'Due', 'Overdue', 'NotApplicable')");
            });

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("VaccinationId").ValueGeneratedOnAdd();

        builder.Property(v => v.VaccineName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Due");
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(v => v.HealthProfileId)
            .HasDatabaseName("IX_Vaccinations_HealthProfileId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(v => new { v.DueDate, v.Status })
            .HasDatabaseName("IX_Vaccinations_DueDate_Status")
            .HasFilter("[IsDeleted] = 0 AND [Status] IN ('Due', 'Overdue') AND [DueDate] IS NOT NULL");

        builder.HasOne(v => v.HealthProfile)
            .WithMany(hp => hp.Vaccinations)
            .HasForeignKey(v => v.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Family)
            .WithMany()
            .HasForeignKey(v => v.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.LinkedDocument)
            .WithMany()
            .HasForeignKey(v => v.LinkedDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
