using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.ConfigureBaseEntity("tblEventReminder", "EventReminderId");

        builder.ToTable("tblEventReminder", table =>
        {
            table.HasCheckConstraint(
                "CK_tblEventReminder_RemindBeforeMinutes",
                "[RemindBeforeMinutes] IN (5, 10, 15, 30, 60, 120, 480, 1440, 4320)");
        });

        builder.Property(r => r.Channel).HasConversion<int>().IsRequired();
        builder.Property(r => r.IsSent).HasDefaultValue(false);

        builder.HasIndex(r => r.CalendarEventId)
            .HasDatabaseName("IDX_tblEventReminder_CalendarEventId");

        builder.HasOne(r => r.CalendarEvent)
            .WithMany(e => e.Reminders)
            .HasForeignKey(r => r.CalendarEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
