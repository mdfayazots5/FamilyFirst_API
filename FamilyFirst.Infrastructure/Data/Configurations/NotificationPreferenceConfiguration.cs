using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ConfigureBaseEntity("tblNotificationPreference", "NotificationPreferenceId");

        // TIME columns stored as DATETIME2 — only the time portion is meaningful.
        // Default anchors to 1900-01-01; values like 22:00, 07:00, 20:00.
        builder.Property(p => p.QuietHoursStartTime)
            .HasDefaultValue(new DateTime(1900, 1, 1, 22, 0, 0));

        builder.Property(p => p.QuietHoursEndTime)
            .HasDefaultValue(new DateTime(1900, 1, 1, 7, 0, 0));

        builder.Property(p => p.MorningDigestTime)
            .HasDefaultValue(new DateTime(1900, 1, 1, 7, 0, 0));

        builder.Property(p => p.EveningDigestTime)
            .HasDefaultValue(new DateTime(1900, 1, 1, 20, 0, 0));

        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("UK_tblNotificationPreference_UserId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
