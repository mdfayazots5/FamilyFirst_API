using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Notification;

public sealed record NotificationDto(
    Guid NotificationId,
    Guid? FamilyId,
    Guid RecipientUserId,
    string Title,
    string Body,
    NotificationPriority Priority,
    NotificationChannel Channel,
    string? ReferenceType,
    Guid? ReferenceId,
    string? DeepLinkPath,
    bool IsRead,
    DateTime? ReadAt,
    bool IsSent,
    DateTime? SentAt,
    string? FcmMessageId,
    bool IsBatched,
    string? BatchGroup,
    DateTime? ScheduledFor,
    DateTime CreatedAt);

public sealed class CreateNotificationRequest
{
    public Guid? FamilyId { get; init; }

    public Guid RecipientUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    public NotificationChannel Channel { get; init; } = NotificationChannel.Push;

    public string? ReferenceType { get; init; }

    public Guid? ReferenceId { get; init; }

    public string? DeepLinkPath { get; init; }

    public bool? IsBatched { get; init; }

    public string? BatchGroup { get; init; }

    public DateTime? ScheduledFor { get; init; }
}

public sealed class EmergencyNotificationRequest
{
    public string? CurrentTaskName { get; init; }
}

public sealed record MarkAllReadResultDto(int Count);
