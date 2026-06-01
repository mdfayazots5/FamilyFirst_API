using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ConfigureBaseEntity("tblCalendarEvent", "CalendarEventId");

        builder.ToTable("tblCalendarEvent", table =>
        {
            table.HasCheckConstraint(
                "CK_tblCalendarEvent_VisibilityScope",
                "[VisibilityScope] IN (N'Family', N'Parent', N'Child', N'Elder', N'Caregiver')");
            table.HasCheckConstraint(
                "CK_tblCalendarEvent_EndDateTime",
                "[EndDateTime] IS NULL OR [EndDateTime] >= [StartDateTime]");
        });

        builder.Property(e => e.EventTitle).HasMaxLength(300).IsRequired();
        builder.Property(e => e.EventType).HasConversion<int>().IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Location).HasMaxLength(300);
        builder.Property(e => e.ColorHex).HasMaxLength(7);
        builder.Property(e => e.RecurrenceRule).HasMaxLength(200);
        builder.Property(e => e.VisibilityScope).HasMaxLength(50).IsRequired().HasDefaultValue("Family");
        builder.Property(e => e.IsAllDay).HasDefaultValue(false);
        builder.Property(e => e.IsRecurring).HasDefaultValue(false);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => new { e.FamilyId, e.StartDateTime })
            .HasDatabaseName("IDX_tblCalendarEvent_FamilyId_StartDateTime")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Family)
            .WithMany()
            .HasForeignKey(e => e.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LinkedChildProfile)
            .WithMany()
            .HasForeignKey(e => e.LinkedChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Reminders)
            .WithOne(r => r.CalendarEvent)
            .HasForeignKey(r => r.CalendarEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
