using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class EventReminder : BaseEntity
{
    public long CalendarEventId { get; set; }

    public Guid EventId => CalendarEvent?.Id ?? Guid.Empty;

    public long FamilyId { get; set; }

    public int RemindBeforeMinutes { get; set; }

    public NotificationChannel Channel { get; set; }

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime ScheduledFor { get; set; }

    public CalendarEvent? CalendarEvent { get; set; }

    public CalendarEvent? Event
    {
        get => CalendarEvent;
        set => CalendarEvent = value;
    }

    public Family? Family { get; set; }
}
