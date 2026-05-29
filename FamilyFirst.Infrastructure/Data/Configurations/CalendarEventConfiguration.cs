using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable(
            "CalendarEvents",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_CalendarEvents_VisibilityScope",
                    "[VisibilityScope] IN (N'Family', N'Parent', N'Child', N'Elder', N'Caregiver')");
                table.HasCheckConstraint(
                    "CK_CalendarEvents_EndDateTime",
                    "[EndDateTime] IS NULL OR [EndDateTime] >= [StartDateTime]");
            });

        builder.HasKey(calendarEvent => calendarEvent.Id);

        builder.Property(calendarEvent => calendarEvent.Id).HasColumnName("EventId").ValueGeneratedOnAdd();
        builder.Property(calendarEvent => calendarEvent.EventTitle).HasMaxLength(300).IsRequired();
        builder.Property(calendarEvent => calendarEvent.EventType).HasConversion<int>().IsRequired();
        builder.Property(calendarEvent => calendarEvent.Description).HasMaxLength(1000);
        builder.Property(calendarEvent => calendarEvent.Location).HasMaxLength(300);
        builder.Property(calendarEvent => calendarEvent.ColorHex).HasMaxLength(7);
        builder.Property(calendarEvent => calendarEvent.RecurrenceRule).HasMaxLength(200);
        builder.Property(calendarEvent => calendarEvent.VisibilityScope).HasMaxLength(50).IsRequired().HasDefaultValue("Family");
        builder.Property(calendarEvent => calendarEvent.IsAllDay).HasDefaultValue(false);
        builder.Property(calendarEvent => calendarEvent.IsRecurring).HasDefaultValue(false);
        builder.Property(calendarEvent => calendarEvent.IsActive).HasDefaultValue(true);
        builder.Property(calendarEvent => calendarEvent.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(calendarEvent => calendarEvent.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(calendarEvent => new { calendarEvent.FamilyId, calendarEvent.StartDateTime })
            .HasDatabaseName("IX_CalendarEvents_FamilyId_StartDateTime")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(calendarEvent => calendarEvent.Family)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(calendarEvent => calendarEvent.CreatedByUser)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(calendarEvent => calendarEvent.LinkedChildProfile)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.LinkedChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(calendarEvent => calendarEvent.Reminders)
            .WithOne(reminder => reminder.Event)
            .HasForeignKey(reminder => reminder.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(calendarEvent => !calendarEvent.IsDeleted);
    }
}
