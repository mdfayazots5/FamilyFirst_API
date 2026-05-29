using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable(
            "TaskItems",
            table =>
            {
                table.HasCheckConstraint("CK_TaskItems_RecurringDaysJson", "ISJSON([RecurringDays]) = 1");
                table.HasCheckConstraint("CK_TaskItems_ActiveDateRange", "[ActiveToDate] IS NULL OR [ActiveToDate] > [ActiveFromDate]");
                table.HasCheckConstraint("CK_TaskItems_PillarTag", "[PillarTag] IS NULL OR [PillarTag] IN ('Study', 'Cleanliness', 'Discipline', 'ScreenControl', 'Responsibility')");
            });

        builder.HasKey(taskItem => taskItem.Id);

        builder.Property(taskItem => taskItem.Id).HasColumnName("TaskId").ValueGeneratedOnAdd();
        builder.Property(taskItem => taskItem.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(taskItem => taskItem.Instructions).HasMaxLength(1000);
        builder.Property(taskItem => taskItem.IconCode).HasMaxLength(50);
        builder.Property(taskItem => taskItem.TimeBlock).HasConversion<int>().IsRequired();
        builder.Property(taskItem => taskItem.DurationMinutes).HasDefaultValue(15);
        builder.Property(taskItem => taskItem.CoinValue).HasDefaultValue(10);
        builder.Property(taskItem => taskItem.PillarTag).HasMaxLength(50);
        builder.Property(taskItem => taskItem.RecurringDays).HasMaxLength(50).IsRequired();
        builder.Property(taskItem => taskItem.ActiveFromDate).HasColumnType("date").IsRequired();
        builder.Property(taskItem => taskItem.ActiveToDate).HasColumnType("date");
        builder.Property(taskItem => taskItem.IsActive).HasDefaultValue(true);
        builder.Property(taskItem => taskItem.IsSystemTemplate).HasDefaultValue(false);
        builder.Property(taskItem => taskItem.TemplateCategory).HasMaxLength(50);
        builder.Property(taskItem => taskItem.AgeGroup).HasMaxLength(50);
        builder.Property(taskItem => taskItem.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(taskItem => taskItem.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(taskItem => new { taskItem.FamilyId, taskItem.ChildProfileId, taskItem.IsActive })
            .HasDatabaseName("IX_TaskItems_FamilyId_ChildProfileId_IsActive")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(taskItem => taskItem.Family)
            .WithMany()
            .HasForeignKey(taskItem => taskItem.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskItem => taskItem.ChildProfile)
            .WithMany()
            .HasForeignKey(taskItem => taskItem.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskItem => taskItem.CreatedByUser)
            .WithMany()
            .HasForeignKey(taskItem => taskItem.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(taskItem => !taskItem.IsDeleted);
    }
}
