using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class Notification
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();

    public Guid? FamilyId { get; set; }

    public Guid RecipientUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public NotificationChannel Channel { get; set; } = NotificationChannel.Push;

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? DeepLinkPath { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public string? FcmMessageId { get; set; }

    public bool IsBatched { get; set; }

    public string? BatchGroup { get; set; }

    public DateTime? ScheduledFor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? RecipientUser { get; set; }

    public Family? Family { get; set; }
}
