using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord>
{
    public void Configure(EntityTypeBuilder<HealthRecord> builder)
    {
        builder.ToTable(
            "HealthRecords",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_HealthRecords_EventType",
                    "[EventType] IN ('Prescription','Vaccination','HospitalVisit','TestReport','DoctorNote','AllergyUpdate')");
            });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("HealthRecordId").ValueGeneratedOnAdd();

        builder.Property(r => r.EventType).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => new { r.HealthProfileId, r.EventDate })
            .HasDatabaseName("IX_HealthRecords_HealthProfileId_EventDate")
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.HealthProfile)
            .WithMany(hp => hp.HealthRecords)
            .HasForeignKey(r => r.HealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.LinkedDocument)
            .WithMany()
            .HasForeignKey(r => r.LinkedDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
