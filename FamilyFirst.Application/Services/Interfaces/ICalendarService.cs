using FamilyFirst.Application.DTOs.Calendar;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ICalendarService
{
    Task<IReadOnlyCollection<EventDto>> ListEventsAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<EventDto> GetEventAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken);

    Task<EventDto> CreateEventAsync(
        Guid currentUserId,
        Guid familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken);

    Task<EventDto> UpdateEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EventDto>> ListUpcomingEventsAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        int days,
        CancellationToken cancellationToken);
}

public interface ICalendarEventRepository
{
    Task<CalendarEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CalendarEvent>> ListByFamilyAsync(
        Guid familyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<CalendarEvent?> GetBirthdayEventAsync(
        Guid familyId,
        Guid linkedChildProfileId,
        DateOnly eventDate,
        CancellationToken cancellationToken);
}

public interface IEventReminderRepository
{
    Task AddEventGraphAsync(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminder> reminders,
        CancellationToken cancellationToken);

    Task UpdateEventGraphAsync(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminder> reminders,
        CancellationToken cancellationToken);

    Task SoftDeleteEventGraphAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EventReminder>> ListDuePendingAsync(DateTime asOfUtc, CancellationToken cancellationToken);

    Task UpdateAsync(EventReminder reminder, CancellationToken cancellationToken);
}
