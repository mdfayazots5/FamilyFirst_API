using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Calendar;

public sealed record EventReminderDto(
    Guid ReminderId,
    int RemindBeforeMinutes,
    NotificationChannel Channel,
    bool IsSent,
    DateTime ScheduledFor);

public sealed class EventReminderRequest
{
    public int RemindBeforeMinutes { get; init; }

    public NotificationChannel Channel { get; init; }
}
