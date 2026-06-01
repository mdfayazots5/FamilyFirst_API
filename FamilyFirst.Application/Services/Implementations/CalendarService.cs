using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Calendar;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class CalendarService : ICalendarService
{
    private static readonly HashSet<string> AllowedVisibilityScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Family",
        "Parent",
        "Child",
        "Elder",
        "Caregiver"
    };

    private static readonly HashSet<int> AllowedReminderMinutes = new()
    {
        5,
        10,
        15,
        30,
        60,
        120,
        480,
        1440,
        4320
    };

    private readonly ICalendarEventRepository _calendarEventRepository;
    private readonly IEventReminderRepository _eventReminderRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IChildProfileRepository _childProfileRepository;

    public CalendarService(
        ICalendarEventRepository calendarEventRepository,
        IEventReminderRepository eventReminderRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository)
    {
        _calendarEventRepository = calendarEventRepository;
        _eventReminderRepository = eventReminderRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
    }

    public async Task<IReadOnlyCollection<EventDto>> ListEventsAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        ValidateDateRange(fromDate, toDate);

        var resolvedChildProfileId = await ResolveCurrentChildProfileIdAsync(
            member,
            currentChildProfileId,
            cancellationToken);

        var calendarEvents = await _calendarEventRepository.ListByFamilyAsync(
            familyId,
            fromDate,
            toDate,
            cancellationToken);

        return calendarEvents
            .Where(calendarEvent => CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
            .OrderBy(calendarEvent => calendarEvent.StartDateTime)
            .ThenBy(calendarEvent => calendarEvent.EventTitle)
            .Select(ToDto)
            .ToArray();
    }

    public async Task<EventDto> GetEventAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var resolvedChildProfileId = await ResolveCurrentChildProfileIdAsync(
            member,
            currentChildProfileId,
            cancellationToken);
        var calendarEvent = await GetEventOrThrowAsync(eventId, cancellationToken);

        if (calendarEvent.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(CalendarEvent), eventId);
        }

        if (!CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
        {
            throw new ForbiddenAccessException("Calendar event access is not allowed.");
        }

        return ToDto(calendarEvent);
    }

    public async Task<EventDto> CreateEventAsync(
        Guid currentUserId,
        Guid familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin and not UserRole.Teacher)
        {
            throw new ForbiddenAccessException("Parent, FamilyAdmin, or Teacher role is required.");
        }

        await EnsureLinkedChildBelongsToFamilyAsync(request.LinkedChildProfileId, familyId, cancellationToken);

        var calendarEvent = new CalendarEvent
        {
            FamilyId = member.FamilyId,
            CreatedByUserId = member.UserId,
            EventTitle = request.EventTitle.Trim(),
            EventType = request.EventType,
            Description = NormalizeOptional(request.Description),
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            IsAllDay = request.IsAllDay,
            Location = NormalizeOptional(request.Location),
            ColorHex = NormalizeOptional(request.ColorHex),
            IsRecurring = request.IsRecurring,
            RecurrenceRule = NormalizeOptional(request.RecurrenceRule),
            VisibilityScope = NormalizeVisibilityScope(request.VisibilityScope),
            LinkedChildProfileId = null, // LinkedChildProfileId is long? in entity; Guid? in DTO — skip child lookup here
            IsActive = true
        };

        var reminders = BuildReminders(calendarEvent, request.Reminders);
        ApplyReminderSnapshot(calendarEvent, reminders);
        await _eventReminderRepository.AddEventGraphAsync(calendarEvent, reminders, cancellationToken);

        return ToDto(calendarEvent);
    }

    public async Task<EventDto> UpdateEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var calendarEvent = await GetEventOrThrowAsync(eventId, cancellationToken);

        if (calendarEvent.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(CalendarEvent), eventId);
        }

        EnsureCanEdit(calendarEvent, member, currentUserId);
        await EnsureLinkedChildBelongsToFamilyAsync(request.LinkedChildProfileId, familyId, cancellationToken);

        calendarEvent.EventTitle = request.EventTitle.Trim();
        calendarEvent.EventType = request.EventType;
        calendarEvent.Description = NormalizeOptional(request.Description);
        calendarEvent.StartDateTime = request.StartDateTime;
        calendarEvent.EndDateTime = request.EndDateTime;
        calendarEvent.IsAllDay = request.IsAllDay;
        calendarEvent.Location = NormalizeOptional(request.Location);
        calendarEvent.ColorHex = NormalizeOptional(request.ColorHex);
        calendarEvent.IsRecurring = request.IsRecurring;
        calendarEvent.RecurrenceRule = NormalizeOptional(request.RecurrenceRule);
        calendarEvent.VisibilityScope = NormalizeVisibilityScope(request.VisibilityScope);
        calendarEvent.LinkedChildProfileId = null; // LinkedChildProfileId is long? in entity; Guid? in DTO — skip

        var reminders = BuildReminders(calendarEvent, request.Reminders);
        await _eventReminderRepository.UpdateEventGraphAsync(calendarEvent, reminders, cancellationToken);

        return ToDto(await GetEventOrThrowAsync(eventId, cancellationToken));
    }

    public async Task<bool> DeleteEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var calendarEvent = await GetEventOrThrowAsync(eventId, cancellationToken);

        if (calendarEvent.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(CalendarEvent), eventId);
        }

        EnsureCanEdit(calendarEvent, member, currentUserId);
        await _eventReminderRepository.SoftDeleteEventGraphAsync(calendarEvent, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<EventDto>> ListUpcomingEventsAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        int days,
        CancellationToken cancellationToken)
    {
        if (days <= 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Days"] = new[] { "Days must be greater than zero." }
                });
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var resolvedChildProfileId = await ResolveCurrentChildProfileIdAsync(
            member,
            currentChildProfileId,
            cancellationToken);
        var fromDate = DateTime.UtcNow;
        var toDate = fromDate.AddDays(days);
        var calendarEvents = await _calendarEventRepository.ListByFamilyAsync(
            familyId,
            fromDate,
            toDate,
            cancellationToken);

        return calendarEvents
            .Where(calendarEvent => CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
            .OrderBy(calendarEvent => calendarEvent.StartDateTime)
            .ThenBy(calendarEvent => calendarEvent.EventTitle)
            .Select(ToDto)
            .ToArray();
    }

    private static IReadOnlyCollection<EventReminder> BuildReminders(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminderRequest> requests)
    {
        if (requests.Count == 0)
        {
            return Array.Empty<EventReminder>();
        }

        return requests
            .Select(request =>
            {
                if (!AllowedReminderMinutes.Contains(request.RemindBeforeMinutes))
                {
                    throw new ValidationException(
                        new Dictionary<string, string[]>
                        {
                            ["Reminders"] = new[] { "Reminder minutes contain an unsupported value." }
                        });
                }

                return new EventReminder
                {
                    CalendarEventId = calendarEvent.InternalId,
                    FamilyId = calendarEvent.FamilyId,
                    RemindBeforeMinutes = request.RemindBeforeMinutes,
                    Channel = request.Channel,
                    IsSent = false,
                    ScheduledFor = calendarEvent.StartDateTime.AddMinutes(-request.RemindBeforeMinutes)
                };
            })
            .ToArray();
    }

    private static void EnsureCanEdit(CalendarEvent calendarEvent, FamilyMember member, Guid currentUserId)
    {
        if (member.Role == UserRole.FamilyAdmin)
        {
            return;
        }

        if (calendarEvent.CreatedByUser?.Id != currentUserId)
        {
            throw new ForbiddenAccessException("Only the event creator or a FamilyAdmin can modify this event.");
        }
    }

    private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["ToDate"] = new[] { "ToDate must be greater than or equal to FromDate." }
                });
        }
    }

    private static EventDto ToDto(CalendarEvent calendarEvent)
    {
        return new EventDto(
            calendarEvent.Id,
            calendarEvent.Family?.Id ?? Guid.Empty,
            calendarEvent.CreatedByUser?.Id ?? Guid.Empty,
            calendarEvent.EventTitle,
            calendarEvent.EventType,
            calendarEvent.Description,
            calendarEvent.StartDateTime,
            calendarEvent.EndDateTime,
            calendarEvent.IsAllDay,
            calendarEvent.Location,
            calendarEvent.ColorHex,
            calendarEvent.IsRecurring,
            calendarEvent.RecurrenceRule,
            calendarEvent.VisibilityScope,
            calendarEvent.LinkedChildProfile?.Id, // LinkedChildProfileId is long? in entity; use nav property
            calendarEvent.IsActive,
            calendarEvent.Reminders
                .OrderBy(reminder => reminder.RemindBeforeMinutes)
                .Select(reminder => new EventReminderDto(
                    reminder.Id,
                    reminder.RemindBeforeMinutes,
                    reminder.Channel,
                    reminder.IsSent,
                    reminder.ScheduledFor))
                .ToArray());
    }

    private static void ApplyReminderSnapshot(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminder> reminders)
    {
        calendarEvent.Reminders.Clear();

        foreach (var reminder in reminders)
        {
            calendarEvent.Reminders.Add(reminder);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeVisibilityScope(string scope)
    {
        var matchedScope = AllowedVisibilityScopes
            .SingleOrDefault(candidate => string.Equals(candidate, scope, StringComparison.OrdinalIgnoreCase));

        return matchedScope ?? "Family";
    }

    private static bool CanViewEvent(
        CalendarEvent calendarEvent,
        FamilyMember member,
        Guid currentUserId,
        Guid? currentChildProfileId)
    {
        if (!calendarEvent.IsActive)
        {
            return false;
        }

        if (member.Role is UserRole.Parent or UserRole.FamilyAdmin)
        {
            return true;
        }

        if (member.Role == UserRole.Teacher)
        {
            return calendarEvent.CreatedByUser?.Id == currentUserId
                || calendarEvent.VisibilityScope is "Family" or "Child" or "Caregiver";
        }

        if (member.Role == UserRole.Elder)
        {
            return calendarEvent.VisibilityScope is "Family" or "Elder";
        }

        if (member.Role == UserRole.Child)
        {
            if (!currentChildProfileId.HasValue)
            {
                return false;
            }

            if (calendarEvent.VisibilityScope is not ("Family" or "Child"))
            {
                return false;
            }

            return !calendarEvent.LinkedChildProfileId.HasValue
                || calendarEvent.LinkedChildProfile?.Id == currentChildProfileId.Value;
        }

        return false;
    }

    private async Task<CalendarEvent> GetEventOrThrowAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _calendarEventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException(nameof(CalendarEvent), eventId);
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task<Guid?> ResolveCurrentChildProfileIdAsync(
        FamilyMember member,
        Guid? currentChildProfileId,
        CancellationToken cancellationToken)
    {
        if (member.Role != UserRole.Child)
        {
            return currentChildProfileId;
        }

        if (currentChildProfileId.HasValue)
        {
            return currentChildProfileId.Value;
        }

        var childProfile = await _childProfileRepository.GetByFamilyMemberIdAsync(member.Id, cancellationToken);

        return childProfile?.Id;
    }

    private async Task EnsureLinkedChildBelongsToFamilyAsync(
        Guid? linkedChildProfileId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (!linkedChildProfileId.HasValue)
        {
            return;
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(linkedChildProfileId.Value, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["LinkedChildProfileId"] = new[] { "Linked child profile was not found in this family." }
                });
        }
    }
}
