using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Calendar;

public sealed record EventDto(
    Guid EventId,
    Guid FamilyId,
    Guid CreatedByUserId,
    string EventTitle,
    EventType EventType,
    string? Description,
    DateTime StartDateTime,
    DateTime? EndDateTime,
    bool IsAllDay,
    string? Location,
    string? ColorHex,
    bool IsRecurring,
    string? RecurrenceRule,
    string VisibilityScope,
    Guid? LinkedChildProfileId,
    bool IsActive,
    IReadOnlyCollection<EventReminderDto> Reminders);
