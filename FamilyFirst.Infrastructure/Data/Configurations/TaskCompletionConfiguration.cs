using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TaskCompletionConfiguration : IEntityTypeConfiguration<TaskCompletion>
{
    public void Configure(EntityTypeBuilder<TaskCompletion> builder)
    {
        builder.ToTable("TaskCompletions");
        builder.HasKey(taskCompletion => taskCompletion.Id);

        builder.Property(taskCompletion => taskCompletion.Id)
            .HasColumnName("CompletionId")
            .ValueGeneratedOnAdd();

        builder.Property(taskCompletion => taskCompletion.ScheduledDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(taskCompletion => taskCompletion.Status)
            .HasConversion<int>()
            .HasDefaultValue(TaskStatus.Pending)
            .IsRequired();

        builder.Property(taskCompletion => taskCompletion.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(taskCompletion => taskCompletion.ReviewNote)
            .HasMaxLength(500);

        builder.Property(taskCompletion => taskCompletion.CoinsAwarded)
            .HasDefaultValue(0);

        builder.Property(taskCompletion => taskCompletion.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(taskCompletion => taskCompletion.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(taskCompletion => new { taskCompletion.TaskId, taskCompletion.ChildProfileId, taskCompletion.ScheduledDate })
            .IsUnique()
            .HasDatabaseName("IX_TaskCompletions_Task_Child_Date")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(taskCompletion => new { taskCompletion.FamilyId, taskCompletion.Status, taskCompletion.ScheduledDate })
            .HasDatabaseName("IX_TaskCompletions_Family_Status_Date")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(taskCompletion => taskCompletion.TaskItem)
            .WithMany()
            .HasForeignKey(taskCompletion => taskCompletion.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskCompletion => taskCompletion.ChildProfile)
            .WithMany()
            .HasForeignKey(taskCompletion => taskCompletion.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskCompletion => taskCompletion.Family)
            .WithMany()
            .HasForeignKey(taskCompletion => taskCompletion.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskCompletion => taskCompletion.ReviewedByUser)
            .WithMany()
            .HasForeignKey(taskCompletion => taskCompletion.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(taskCompletion => !taskCompletion.IsDeleted);
    }
}
