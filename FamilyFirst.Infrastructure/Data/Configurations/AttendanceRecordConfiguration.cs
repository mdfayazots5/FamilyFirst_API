using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id).HasColumnName("RecordId").ValueGeneratedOnAdd();
        builder.Property(record => record.Status).HasConversion<int>().IsRequired();
        builder.Property(record => record.TeacherComment).HasMaxLength(500);
        builder.Property(record => record.MarkedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(record => record.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(record => record.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(record => new { record.SessionId, record.ChildProfileId })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceRecords_Session_Child")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(record => new { record.FamilyId, record.ChildProfileId })
            .HasDatabaseName("IX_AttendanceRecords_FamilyId_ChildProfileId");

        builder.HasOne(record => record.Session)
            .WithMany()
            .HasForeignKey(record => record.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.ChildProfile)
            .WithMany()
            .HasForeignKey(record => record.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Family)
            .WithMany()
            .HasForeignKey(record => record.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.MarkedByUser)
            .WithMany()
            .HasForeignKey(record => record.MarkedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.EditedByUser)
            .WithMany()
            .HasForeignKey(record => record.EditedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(record => !record.IsDeleted);
    }
}
