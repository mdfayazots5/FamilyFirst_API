using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherFeedbackConfiguration : IEntityTypeConfiguration<TeacherFeedback>
{
    public void Configure(EntityTypeBuilder<TeacherFeedback> builder)
    {
        builder.ConfigureBaseEntity("tblTeacherFeedback", "TeacherFeedbackId");

        builder.ToTable("tblTeacherFeedback", table =>
        {
            table.HasCheckConstraint("CK_tblTeacherFeedback_WeeklySummaryJson", "[WeeklySummaryJson] IS NULL OR ISJSON([WeeklySummaryJson]) = 1");
            table.HasCheckConstraint("CK_tblTeacherFeedback_ResolutionStatus", "[ResolutionStatus] IN (N'Open', N'Acknowledged', N'Resolved')");
        });

        builder.Property(f => f.FeedbackType).HasConversion<int>().IsRequired();
        builder.Property(f => f.Severity).HasConversion<int?>();
        builder.Property(f => f.Subject).HasMaxLength(512);
        builder.Property(f => f.Message).HasMaxLength(2048).IsRequired();
        builder.Property(f => f.ParentResponseText).HasMaxLength(1024);
        builder.Property(f => f.ResolutionStatus).HasMaxLength(24).IsRequired().HasDefaultValue("Open");
        builder.Property(f => f.IsAcknowledged).HasDefaultValue(false);

        // IsEditable is a computed column — uses DateCreated (new SQL Format column name)
        builder.Property(f => f.IsEditable)
            .HasComputedColumnSql("CASE WHEN DATEDIFF(HOUR, [DateCreated], GETDATE()) < 24 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END", false);

        builder.HasIndex(f => new { f.FamilyId, f.ChildProfileId, f.FeedbackType })
            .HasDatabaseName("IDX_tblTeacherFeedback_FamilyId_ChildProfileId_FeedbackType")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(f => f.TeacherProfile)
            .WithMany()
            .HasForeignKey(f => f.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.ChildProfile)
            .WithMany()
            .HasForeignKey(f => f.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Family)
            .WithMany()
            .HasForeignKey(f => f.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.AttendanceSession)
            .WithMany()
            .HasForeignKey(f => f.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.CommentTemplate)
            .WithMany()
            .HasForeignKey(f => f.CommentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(f => f.AcknowledgedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
