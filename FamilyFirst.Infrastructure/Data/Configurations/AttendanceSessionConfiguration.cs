using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ConfigureBaseEntity("tblAttendanceSession", "AttendanceSessionId");

        builder.ToTable("tblAttendanceSession", table =>
        {
            table.HasCheckConstraint("CK_tblAttendanceSession_TimeRange", "[EndTime] IS NULL OR [EndTime] > [StartTime]");
            table.HasCheckConstraint("CK_tblAttendanceSession_RecurringDaysJson", "[RecurringDays] IS NULL OR ISJSON([RecurringDays]) = 1");
            table.HasCheckConstraint("CK_tblAttendanceSession_RecurringDaysRequired", "[IsRecurring] = 0 OR [RecurringDays] IS NOT NULL");
        });

        builder.Property(s => s.SessionName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.SubjectName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.BatchName).HasMaxLength(128);
        builder.Property(s => s.RecurringDays).HasMaxLength(64);
        builder.Property(s => s.IsSubmitted).HasDefaultValue(false);
        builder.Property(s => s.IsRecurring).HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => new { s.TeacherProfileId, s.ScheduledDate })
            .HasDatabaseName("IDX_tblAttendanceSession_TeacherProfileId_ScheduledDate")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasIndex(s => new { s.FamilyId, s.ScheduledDate })
            .HasDatabaseName("IDX_tblAttendanceSession_FamilyId_ScheduledDate")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasOne(s => s.TeacherProfile)
            .WithMany()
            .HasForeignKey(s => s.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
