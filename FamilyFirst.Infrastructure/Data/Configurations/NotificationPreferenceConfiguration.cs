using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(preference => preference.PreferenceId);

        builder.Property(preference => preference.PreferenceId)
            .HasColumnName("PreferenceId")
            .ValueGeneratedOnAdd();

        builder.Property(preference => preference.QuietHoursStartTime)
            .HasColumnType("time")
            .HasDefaultValue(new TimeOnly(22, 0));

        builder.Property(preference => preference.QuietHoursEndTime)
            .HasColumnType("time")
            .HasDefaultValue(new TimeOnly(7, 0));

        builder.Property(preference => preference.MorningDigestTime)
            .HasColumnType("time")
            .HasDefaultValue(new TimeOnly(7, 0));

        builder.Property(preference => preference.EveningDigestTime)
            .HasColumnType("time")
            .HasDefaultValue(new TimeOnly(20, 0));

        builder.Property(preference => preference.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(preference => preference.UserId)
            .IsUnique()
            .HasDatabaseName("UX_NotificationPreferences_UserId");

        builder.HasOne(preference => preference.User)
            .WithMany()
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(preference => preference.Family)
            .WithMany()
            .HasForeignKey(preference => preference.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
