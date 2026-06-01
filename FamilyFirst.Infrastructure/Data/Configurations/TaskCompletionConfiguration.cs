using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TaskCompletionConfiguration : IEntityTypeConfiguration<TaskCompletion>
{
    public void Configure(EntityTypeBuilder<TaskCompletion> builder)
    {
        builder.ConfigureBaseEntity("tblTaskCompletion", "TaskCompletionId");

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .HasDefaultValue(TaskStatus.Pending)
            .IsRequired();

        builder.Property(c => c.PhotoUrl).HasMaxLength(500);
        builder.Property(c => c.ReviewNote).HasMaxLength(500);
        builder.Property(c => c.CoinsAwarded).HasDefaultValue(0);

        builder.HasIndex(c => new { c.TaskItemId, c.ChildProfileId, c.ScheduledDate })
            .IsUnique()
            .HasDatabaseName("UK_tblTaskCompletion_TaskItemId_ChildProfileId_ScheduledDate")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.TaskItem)
            .WithMany()
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ChildProfile)
            .WithMany()
            .HasForeignKey(c => c.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReviewedByUser)
            .WithMany()
            .HasForeignKey(c => c.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
