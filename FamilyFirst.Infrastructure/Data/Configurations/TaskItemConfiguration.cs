using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ConfigureBaseEntity("tblTaskItem", "TaskItemId");

        builder.ToTable("tblTaskItem", table =>
        {
            table.HasCheckConstraint("CK_tblTaskItem_RecurringDaysJson", "ISJSON([RecurringDays]) = 1");
            table.HasCheckConstraint("CK_tblTaskItem_ActiveDateRange", "[ActiveToDate] IS NULL OR [ActiveToDate] > [ActiveFromDate]");
            table.HasCheckConstraint("CK_tblTaskItem_PillarTag", "[PillarTag] IS NULL OR [PillarTag] IN ('Study', 'Cleanliness', 'Discipline', 'ScreenControl', 'Responsibility')");
        });

        builder.Property(t => t.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Instructions).HasMaxLength(1000);
        builder.Property(t => t.IconCode).HasMaxLength(50);
        builder.Property(t => t.TimeBlock).HasConversion<int>().IsRequired();
        builder.Property(t => t.DurationMinutes).HasDefaultValue(15);
        builder.Property(t => t.CoinValue).HasDefaultValue(10);
        builder.Property(t => t.PillarTag).HasMaxLength(50);
        builder.Property(t => t.RecurringDays).HasMaxLength(64).IsRequired();
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.IsSystemTemplate).HasDefaultValue(false);
        builder.Property(t => t.TemplateCategory).HasMaxLength(50);
        builder.Property(t => t.AgeGroup).HasMaxLength(50);

        builder.HasIndex(t => new { t.FamilyId, t.ChildProfileId, t.IsActive })
            .HasDatabaseName("IDX_tblTaskItem_FamilyId_ChildProfileId_IsActive")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ChildProfile)
            .WithMany()
            .HasForeignKey(t => t.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
