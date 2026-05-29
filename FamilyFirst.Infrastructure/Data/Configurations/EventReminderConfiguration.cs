using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.ToTable(
            "EventReminders",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_EventReminders_RemindBeforeMinutes",
                    "[RemindBeforeMinutes] IN (5, 10, 15, 30, 60, 120, 480, 1440, 4320)");
            });

        builder.HasKey(reminder => reminder.Id);

        builder.Property(reminder => reminder.Id).HasColumnName("ReminderId").ValueGeneratedOnAdd();
        builder.Property(reminder => reminder.Channel).HasConversion<int>().IsRequired();
        builder.Property(reminder => reminder.IsSent).HasDefaultValue(false);
        builder.Property(reminder => reminder.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(reminder => reminder.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(reminder => reminder.Event)
            .WithMany(calendarEvent => calendarEvent.Reminders)
            .HasForeignKey(reminder => reminder.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reminder => reminder.Family)
            .WithMany()
            .HasForeignKey(reminder => reminder.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(reminder => !reminder.IsDeleted);
    }
}
