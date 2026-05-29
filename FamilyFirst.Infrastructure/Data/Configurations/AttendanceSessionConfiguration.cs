using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable(
            "AttendanceSessions",
            table =>
            {
                table.HasCheckConstraint("CK_AttendanceSessions_TimeRange", "[EndTime] IS NULL OR [EndTime] > [StartTime]");
                table.HasCheckConstraint("CK_AttendanceSessions_RecurringDaysJson", "[RecurringDays] IS NULL OR ISJSON([RecurringDays]) = 1");
                table.HasCheckConstraint("CK_AttendanceSessions_RecurringDaysRequired", "[IsRecurring] = 0 OR [RecurringDays] IS NOT NULL");
            });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id).HasColumnName("SessionId").ValueGeneratedOnAdd();
        builder.Property(session => session.SessionName).HasMaxLength(200).IsRequired();
        builder.Property(session => session.SubjectName).HasMaxLength(200).IsRequired();
        builder.Property(session => session.BatchName).HasMaxLength(100);
        builder.Property(session => session.ScheduledDate).HasColumnType("date").IsRequired();
        builder.Property(session => session.StartTime).HasColumnType("time").IsRequired();
        builder.Property(session => session.EndTime).HasColumnType("time");
        builder.Property(session => session.IsSubmitted).HasDefaultValue(false);
        builder.Property(session => session.IsRecurring).HasDefaultValue(false);
        builder.Property(session => session.RecurringDays).HasMaxLength(50);
        builder.Property(session => session.IsActive).HasDefaultValue(true);
        builder.Property(session => session.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(session => session.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(session => new { session.TeacherProfileId, session.ScheduledDate })
            .HasDatabaseName("IX_AttendanceSessions_TeacherProfileId_ScheduledDate")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasIndex(session => new { session.FamilyId, session.ScheduledDate })
            .HasDatabaseName("IX_AttendanceSessions_FamilyId_ScheduledDate")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasOne(session => session.TeacherProfile)
            .WithMany()
            .HasForeignKey(session => session.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.Family)
            .WithMany()
            .HasForeignKey(session => session.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(session => !session.IsDeleted);
    }
}
