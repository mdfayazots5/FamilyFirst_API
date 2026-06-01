using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord>
{
    public void Configure(EntityTypeBuilder<HealthRecord> builder)
    {
        builder.ConfigureBaseEntity("tblHealthRecord", "HealthRecordId");

        builder.ToTable("tblHealthRecord", table =>
        {
            table.HasCheckConstraint(
                "CK_tblHealthRecord_EventType",
                "[EventType] IN ('Prescription','Vaccination','HospitalVisit','TestReport','DoctorNote','AllergyUpdate')");
        });

        builder.Property(r => r.EventType).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.HasIndex(r => new { r.HealthProfileId, r.EventDate })
            .HasDatabaseName("IDX_tblHealthRecord_HealthProfileId_EventDate")
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

        builder.HasOne(r => r.LinkedVaultDocument)
            .WithMany()
            .HasForeignKey(r => r.LinkedVaultDocumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
