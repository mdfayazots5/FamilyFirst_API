using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.NotificationId);

        builder.Property(notification => notification.NotificationId)
            .HasColumnName("NotificationId")
            .ValueGeneratedOnAdd();

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(notification => notification.Priority)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.NotificationPriority.Normal);

        builder.Property(notification => notification.Channel)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.NotificationChannel.Push);

        builder.Property(notification => notification.ReferenceType)
            .HasMaxLength(50);

        builder.Property(notification => notification.DeepLinkPath)
            .HasMaxLength(300);

        builder.Property(notification => notification.FcmMessageId)
            .HasMaxLength(200);

        builder.Property(notification => notification.BatchGroup)
            .HasMaxLength(50);

        builder.Property(notification => notification.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(notification => new
            {
                notification.RecipientUserId,
                notification.IsRead,
                notification.IsSent
            })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_IsSent");

        builder.HasOne(notification => notification.RecipientUser)
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.Family)
            .WithMany()
            .HasForeignKey(notification => notification.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
