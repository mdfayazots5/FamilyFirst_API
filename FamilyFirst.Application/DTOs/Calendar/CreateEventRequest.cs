using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Calendar;

public sealed class CreateEventRequest
{
    public string EventTitle { get; init; } = string.Empty;

    public EventType EventType { get; init; }

    public string? Description { get; init; }

    public DateTime StartDateTime { get; init; }

    public DateTime? EndDateTime { get; init; }

    public bool IsAllDay { get; init; }

    public string? Location { get; init; }

    public string? ColorHex { get; init; }

    public string VisibilityScope { get; init; } = "Family";

    public bool IsRecurring { get; init; }

    public string? RecurrenceRule { get; init; }

    public Guid? LinkedChildProfileId { get; init; }

    public IReadOnlyCollection<EventReminderRequest> Reminders { get; init; } = Array.Empty<EventReminderRequest>();
}
