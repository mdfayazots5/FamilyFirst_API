using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ConfigureBaseEntity("tblNotification", "NotificationId");

        builder.Property(n => n.Title).HasMaxLength(256).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1024).IsRequired();
        builder.Property(n => n.Priority).HasConversion<int>().HasDefaultValue(Domain.Enums.NotificationPriority.Normal);
        builder.Property(n => n.Channel).HasConversion<int>().HasDefaultValue(Domain.Enums.NotificationChannel.Push);
        builder.Property(n => n.ReferenceType).HasMaxLength(64);
        builder.Property(n => n.DeepLinkPath).HasMaxLength(512);
        builder.Property(n => n.FcmMessageId).HasMaxLength(256);
        builder.Property(n => n.BatchGroup).HasMaxLength(64);

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.IsSent })
            .HasDatabaseName("IDX_tblNotification_RecipientUserId_IsRead_IsSent");

        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Family)
            .WithMany()
            .HasForeignKey(n => n.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
