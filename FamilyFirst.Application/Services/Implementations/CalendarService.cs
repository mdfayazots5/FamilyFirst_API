using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Calendar;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

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
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public CalendarService(
        ICalendarEventRepository calendarEventRepository,
        IEventReminderRepository eventReminderRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _calendarEventRepository = calendarEventRepository;
        _eventReminderRepository = eventReminderRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
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
        await ValidateDateRangeAsync(fromDate, toDate, cancellationToken);

        var resolvedChildProfileId = await ResolveCurrentChildProfileIdAsync(
            member,
            currentChildProfileId,
            cancellationToken);

        var calendarEvents = await _calendarEventRepository.ListByFamilyAsync(
            familyId,
            fromDate,
            toDate,
            cancellationToken);

        var response = calendarEvents
            .Where(calendarEvent => CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
            .OrderBy(calendarEvent => calendarEvent.StartDateTime)
            .ThenBy(calendarEvent => calendarEvent.EventTitle)
            .Select(ToDto)
            .ToArray();
        LogApiCall(nameof(ListEventsAsync), new { currentUserId, familyId, fromDate, toDate }, new { Count = response.Length });
        return response;
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
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        if (!CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var response = ToDto(calendarEvent);
        LogApiCall(nameof(GetEventAsync), new { currentUserId, familyId, eventId }, new { response.EventId });
        return response;
    }

    public async Task<EventDto> CreateEventAsync(
        Guid currentUserId,
        Guid familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin and not UserRole.Teacher)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var linkedChildInternalId = await ResolveLinkedChildInternalIdAsync(request.LinkedChildProfileId, familyId, cancellationToken);

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
            LinkedChildProfileId = linkedChildInternalId,
            IsActive = true
        };

        var reminders = await BuildRemindersAsync(calendarEvent, request.Reminders, cancellationToken);
        ApplyReminderSnapshot(calendarEvent, reminders);
        await _eventReminderRepository.AddEventGraphAsync(calendarEvent, reminders, cancellationToken);

        var response = ToDto(calendarEvent);
        LogApiCall(nameof(CreateEventAsync), new { currentUserId, familyId, request.EventTitle, request.LinkedChildProfileId }, new { response.EventId });
        return response;
    }

    public async Task<EventDto> UpdateEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var calendarEvent = await GetEventOrThrowAsync(eventId, cancellationToken);

        if (calendarEvent.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        await EnsureCanEditAsync(calendarEvent, member, currentUserId, cancellationToken);
        var linkedChildInternalId = await ResolveLinkedChildInternalIdAsync(request.LinkedChildProfileId, familyId, cancellationToken);

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
        calendarEvent.LinkedChildProfileId = linkedChildInternalId;

        var reminders = await BuildRemindersAsync(calendarEvent, request.Reminders, cancellationToken);
        await _eventReminderRepository.UpdateEventGraphAsync(calendarEvent, reminders, cancellationToken);

        var response = ToDto(await GetEventOrThrowAsync(eventId, cancellationToken));
        LogApiCall(nameof(UpdateEventAsync), new { currentUserId, familyId, eventId, request.LinkedChildProfileId }, new { response.EventId });
        return response;
    }

    public async Task<bool> DeleteEventAsync(
        Guid currentUserId,
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);
        var calendarEvent = await GetEventOrThrowAsync(eventId, cancellationToken);

        if (calendarEvent.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        await EnsureCanEditAsync(calendarEvent, member, currentUserId, cancellationToken);
        await _eventReminderRepository.SoftDeleteEventGraphAsync(calendarEvent, cancellationToken);

        LogApiCall(nameof(DeleteEventAsync), new { currentUserId, familyId, eventId }, new { Deleted = true });
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
                    ["Days"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
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

        var response = calendarEvents
            .Where(calendarEvent => CanViewEvent(calendarEvent, member, currentUserId, resolvedChildProfileId))
            .OrderBy(calendarEvent => calendarEvent.StartDateTime)
            .ThenBy(calendarEvent => calendarEvent.EventTitle)
            .Select(ToDto)
            .ToArray();
        LogApiCall(nameof(ListUpcomingEventsAsync), new { currentUserId, familyId, days }, new { Count = response.Length });
        return response;
    }

    private async Task<IReadOnlyCollection<EventReminder>> BuildRemindersAsync(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminderRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return Array.Empty<EventReminder>();
        }

        var reminders = new List<EventReminder>(requests.Count);

        foreach (var request in requests)
        {
            if (!AllowedReminderMinutes.Contains(request.RemindBeforeMinutes))
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["Reminders"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                    });
            }

            reminders.Add(new EventReminder
            {
                CalendarEventId = calendarEvent.InternalId,
                FamilyId = calendarEvent.FamilyId,
                RemindBeforeMinutes = request.RemindBeforeMinutes,
                Channel = request.Channel,
                IsSent = false,
                ScheduledFor = calendarEvent.StartDateTime.AddMinutes(-request.RemindBeforeMinutes)
            });
        }

        return reminders;
    }

    private async Task EnsureCanEditAsync(CalendarEvent calendarEvent, FamilyMember member, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (member.Role == UserRole.FamilyAdmin)
        {
            return;
        }

        if (calendarEvent.CreatedByUser?.Id != currentUserId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task ValidateDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["ToDate"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
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
            calendarEvent.LinkedChildProfile?.Id,
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
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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

    private async Task<long?> ResolveLinkedChildInternalIdAsync(
        Guid? linkedChildProfileId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (!linkedChildProfileId.HasValue)
        {
            return null;
        }

        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile,
            linkedChildProfileId.Value.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedChildId.HasValue)
        {
            throw await CreateLinkedChildValidationExceptionAsync(cancellationToken);
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(linkedChildProfileId.Value, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw await CreateLinkedChildValidationExceptionAsync(cancellationToken);
        }

        return resolvedChildId.Value;
    }

    private async Task<long> GetFamilyInternalIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return resolvedFamilyId.Value;
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Calendar,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await _errorCodeService.GetMessageAsync(
            FamilyFirstErrorCode.Invalid_MasterData,
            cancellationToken: cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(MasterDataCodes)] = new[] { message }
        });
    }

    private async Task<ValidationException> CreateLinkedChildValidationExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            ["LinkedChildProfileId"] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }
}
