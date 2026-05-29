using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TeacherFeedbackConfiguration : IEntityTypeConfiguration<TeacherFeedback>
{
    public void Configure(EntityTypeBuilder<TeacherFeedback> builder)
    {
        builder.ToTable(
            "TeacherFeedback",
            table =>
            {
                table.HasCheckConstraint("CK_TeacherFeedback_WeeklySummaryJson", "[WeeklySummaryJson] IS NULL OR ISJSON([WeeklySummaryJson]) = 1");
                table.HasCheckConstraint("CK_TeacherFeedback_ResolutionStatus", "[ResolutionStatus] IN (N'Open', N'Acknowledged', N'Resolved')");
            });

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.Id).HasColumnName("FeedbackId").ValueGeneratedOnAdd();
        builder.Property(feedback => feedback.FeedbackType).HasConversion<int>().IsRequired();
        builder.Property(feedback => feedback.Severity).HasConversion<int?>();
        builder.Property(feedback => feedback.Subject).HasMaxLength(300);
        builder.Property(feedback => feedback.Message).HasMaxLength(2000).IsRequired();
        builder.Property(feedback => feedback.ParentResponseText).HasMaxLength(1000);
        builder.Property(feedback => feedback.ResolutionStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Open");
        builder.Property(feedback => feedback.IsAcknowledged).HasDefaultValue(false);
        builder.Property(feedback => feedback.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(feedback => feedback.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(feedback => feedback.IsEditable)
            .HasComputedColumnSql("CASE WHEN DATEDIFF(HOUR, [CreatedAt], GETUTCDATE()) < 24 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END", false);

        builder.HasIndex(feedback => new { feedback.FamilyId, feedback.ChildProfileId, feedback.FeedbackType })
            .HasDatabaseName("IX_TeacherFeedback_FamilyId_ChildProfileId_FeedbackType")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(feedback => feedback.TeacherProfile)
            .WithMany()
            .HasForeignKey(feedback => feedback.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.ChildProfile)
            .WithMany()
            .HasForeignKey(feedback => feedback.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.Family)
            .WithMany()
            .HasForeignKey(feedback => feedback.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.Session)
            .WithMany()
            .HasForeignKey(feedback => feedback.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.CommentTemplate)
            .WithMany()
            .HasForeignKey(feedback => feedback.CommentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(feedback => feedback.AcknowledgedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(feedback => !feedback.IsDeleted);
    }
}
