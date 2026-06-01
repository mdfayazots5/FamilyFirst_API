using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ConfigureBaseEntity("tblAttendanceRecord", "AttendanceRecordId");

        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.TeacherComment).HasMaxLength(512);
        builder.Property(r => r.MarkedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(r => new { r.AttendanceSessionId, r.ChildProfileId })
            .IsUnique()
            .HasDatabaseName("UK_tblAttendanceRecord_AttendanceSessionId_ChildProfileId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(r => new { r.FamilyId, r.ChildProfileId })
            .HasDatabaseName("IDX_tblAttendanceRecord_FamilyId_ChildProfileId");

        builder.HasIndex(r => r.AttendanceSessionId)
            .HasDatabaseName("IDX_tblAttendanceRecord_AttendanceSessionId");

        builder.HasOne(r => r.AttendanceSession)
            .WithMany()
            .HasForeignKey(r => r.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ChildProfile)
            .WithMany()
            .HasForeignKey(r => r.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.MarkedByUser)
            .WithMany()
            .HasForeignKey(r => r.MarkedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.EditedByUser)
            .WithMany()
            .HasForeignKey(r => r.EditedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
