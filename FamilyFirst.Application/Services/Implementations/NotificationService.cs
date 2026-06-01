using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    public const string MorningDigestBatchGroup = "MorningDigest";
    public const string EveningDigestBatchGroup = "EveningDigest";
    public const string SuppressedFcmMessageId = "suppressed";

    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IFamilyAdminConfigRepository _familyAdminConfigRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IUserRepository _userRepository;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPreferenceService notificationPreferenceService,
        IUserRepository userRepository,
        IFamilyAdminConfigRepository familyAdminConfigRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        IPushNotificationService pushNotificationService)
    {
        _notificationRepository = notificationRepository;
        _notificationPreferenceService = notificationPreferenceService;
        _userRepository = userRepository;
        _familyAdminConfigRepository = familyAdminConfigRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<IReadOnlyCollection<NotificationDto>> CreateManyAsync(
        IReadOnlyCollection<CreateNotificationRequest> requests,
        CancellationToken cancellationToken)
    {
        var notifications = new List<NotificationDto>(requests.Count);

        foreach (var request in requests)
        {
            notifications.Add(await CreateAsync(request, cancellationToken));
        }

        return notifications;
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var recipient = await _userRepository.GetByIdAsync(request.RecipientUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.RecipientUserId);
        var preference = await _notificationPreferenceService.GetOrCreatePreferencesAsync(
            request.RecipientUserId,
            cancellationToken);
        var utcNow = DateTime.UtcNow;
        var ruleOverride = await ResolveRuleOverrideAsync(
            request.FamilyId,
            request.ReferenceType,
            cancellationToken);
        var notification = new Notification
        {
            FamilyId = null, // long? in entity; Guid? from request — skip direct assignment
            RecipientUserId = recipient.InternalId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Priority = request.Priority,
            Channel = request.Channel,
            ReferenceType = NormalizeOptional(request.ReferenceType),
            ReferenceId = null, // long? in entity; Guid? from request — skip
            DeepLinkPath = NormalizeOptional(request.DeepLinkPath)
        };

        ApplyDeliveryMetadata(notification, request, preference, utcNow);
        ApplyRuleOverride(notification, ruleOverride, utcNow);
        await _notificationRepository.AddAsync(notification, cancellationToken);

        if ((ruleOverride is not null && !ruleOverride.IsEnabled)
            || !IsPreferenceEnabled(preference, notification.ReferenceType))
        {
            notification.IsSent = true;
            notification.SentAt = utcNow;
            notification.FcmMessageId = SuppressedFcmMessageId;
            await _notificationRepository.UpdateAsync(notification, cancellationToken);

            return ToDto(notification);
        }

        if (notification.Priority == NotificationPriority.Urgent
            || (notification.Priority == NotificationPriority.High
                && !notification.IsBatched
                && !notification.ScheduledFor.HasValue))
        {
            await TrySendInlineAsync(notification, recipient, cancellationToken);
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }

        return ToDto(notification);
    }

    public async Task<PaginatedList<NotificationDto>> ListNotificationsAsync(
        Guid currentUserId,
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isRead,
        CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };
        var notifications = await _notificationRepository.ListByRecipientAsync(userId, isRead, cancellationToken);

        return PaginatedList<NotificationDto>.Create(
            notifications
                .OrderByDescending(notification => notification.DateCreated)
                .Select(ToDto),
            normalizedPageNumber,
            normalizedPageSize);
    }

    public async Task<bool> MarkReadAsync(
        Guid currentUserId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        if (notification.RecipientUser?.Id != userId)
        {
            throw new ForbiddenAccessException("Notification access is forbidden.");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        return true;
    }

    public async Task<MarkAllReadResultDto> MarkAllReadAsync(
        Guid currentUserId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);
        var count = await _notificationRepository.MarkAllReadAsync(userId, cancellationToken);

        return new MarkAllReadResultDto(count);
    }

    public async Task<IReadOnlyCollection<NotificationDto>> SendEmergencyAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        EmergencyNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentChildProfileId.HasValue)
        {
            throw new ForbiddenAccessException("Child profile context is required.");
        }

        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException("Child role is required.");
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(currentChildProfileId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(ChildProfile), currentChildProfileId.Value);

        if (childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), currentChildProfileId.Value);
        }

        var childName = childProfile.User?.FullName
            ?? childProfile.FamilyMember?.User?.FullName
            ?? "Child";
        var currentTaskName = NormalizeOptional(request.CurrentTaskName);
        var title = currentTaskName is null ? "Emergency alert" : "Task help request";
        var body = currentTaskName is null
            ? $"{childName} pressed Emergency. Check on them now."
            : $"{childName} needs help with task: {currentTaskName}.";
        var recipients = (await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken))
            .Where(familyMember => familyMember.Role is UserRole.Parent or UserRole.FamilyAdmin)
            .Select(familyMember => familyMember.UserId)
            .Distinct()
            .ToArray();
        var deepLinkPath = $"/families/{familyId}";

        return await CreateManyAsync(
            recipients
                .Select(recipientUserId => new CreateNotificationRequest
                {
                    FamilyId = familyId,
                    RecipientUserId = recipientUserId,
                    Title = title,
                    Body = body,
                    Priority = NotificationPriority.Urgent,
                    ReferenceType = "System",
                    ReferenceId = childProfile.Id,
                    DeepLinkPath = deepLinkPath
                })
                .ToArray(),
            cancellationToken);
    }

    private async Task TrySendInlineAsync(
        Notification notification,
        User recipient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient.FcmToken))
        {
            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;
            notification.FcmMessageId = SuppressedFcmMessageId;
            return;
        }

        var delivered = await _pushNotificationService.SendPushAsync(
            recipient.FcmToken,
            notification.Title,
            notification.Body,
            cancellationToken,
            CreateDataPayload(notification));

        if (delivered)
        {
            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;
        }
    }

    private static IReadOnlyDictionary<string, string>? CreateDataPayload(Notification notification)
    {
        var values = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(notification.DeepLinkPath))
        {
            values["deepLink"] = notification.DeepLinkPath;
        }

        if (notification.FamilyId.HasValue)
        {
            values["familyId"] = notification.FamilyId.Value.ToString();
        }

        if (notification.ReferenceId.HasValue)
        {
            values["referenceId"] = notification.ReferenceId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(notification.ReferenceType))
        {
            values["referenceType"] = notification.ReferenceType;
        }

        return values.Count == 0 ? null : values;
    }

    private static void ApplyDeliveryMetadata(
        Notification notification,
        CreateNotificationRequest request,
        NotificationPreference preference,
        DateTime utcNow)
    {
        notification.IsBatched = request.IsBatched ?? notification.Priority switch
        {
            NotificationPriority.Low => true,
            NotificationPriority.Normal => true,
            NotificationPriority.High => preference.QuietHoursEnabled && IsWithinQuietHours(utcNow, preference),
            _ => false
        };

        notification.BatchGroup = request.BatchGroup;
        notification.ScheduledFor = request.ScheduledFor;

        if (notification.IsBatched)
        {
            notification.BatchGroup ??= notification.Priority switch
            {
                NotificationPriority.Low => EveningDigestBatchGroup,
                NotificationPriority.Normal => MorningDigestBatchGroup,
                NotificationPriority.High => MorningDigestBatchGroup,
                _ => null
            };

            notification.ScheduledFor ??= notification.BatchGroup switch
            {
                MorningDigestBatchGroup => ResolveNextDigestUtc(utcNow, TimeOnly.FromDateTime(preference.MorningDigestTime)),
                EveningDigestBatchGroup => ResolveNextDigestUtc(utcNow, TimeOnly.FromDateTime(preference.EveningDigestTime)),
                _ => utcNow
            };
        }
        else
        {
            notification.BatchGroup = null;
            notification.ScheduledFor = request.ScheduledFor;
        }
    }

    private static void ApplyRuleOverride(
        Notification notification,
        NotificationRuleOverride? ruleOverride,
        DateTime utcNow)
    {
        if (ruleOverride is null)
        {
            return;
        }

        if (ruleOverride.PriorityOverride.HasValue)
        {
            notification.Priority = ruleOverride.PriorityOverride.Value;
        }

        if (ruleOverride.DeliveryDelayMinutes.GetValueOrDefault() > 0)
        {
            notification.ScheduledFor = utcNow.AddMinutes(ruleOverride.DeliveryDelayMinutes.Value);
        }
    }

    private static DateTime ResolveNextDigestUtc(DateTime utcNow, TimeOnly digestTime)
    {
        var date = DateOnly.FromDateTime(utcNow);
        var currentTime = TimeOnly.FromDateTime(utcNow);
        var digestDate = currentTime <= digestTime
            ? date
            : date.AddDays(1);

        return digestDate.ToDateTime(digestTime, DateTimeKind.Utc);
    }

    private static bool IsWithinQuietHours(DateTime utcNow, NotificationPreference preference)
    {
        var currentTime = TimeOnly.FromDateTime(utcNow);
        var start = TimeOnly.FromDateTime(preference.QuietHoursStartTime);
        var end = TimeOnly.FromDateTime(preference.QuietHoursEndTime);

        if (start == end)
        {
            return true;
        }

        if (start < end)
        {
            return currentTime >= start && currentTime < end;
        }

        return currentTime >= start || currentTime < end;
    }

    private static bool IsPreferenceEnabled(NotificationPreference preference, string? referenceType)
    {
        return referenceType switch
        {
            "Attendance" => preference.AttendanceAlerts,
            "Feedback" => preference.FeedbackAlerts,
            "Task" => preference.TaskVerificationAlerts,
            "Reward" => preference.RewardAlerts,
            "Calendar" => preference.CalendarAlerts,
            "WeeklyDigest" => preference.WeeklyDigest,
            "System" => true,
            _ => true
        };
    }

    private static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Family?.Id,
            notification.RecipientUser?.Id ?? Guid.Empty,
            notification.Title,
            notification.Body,
            notification.Priority,
            notification.Channel,
            notification.ReferenceType,
            null, // ReferenceId: long? in entity, Guid? in DTO — not mappable directly
            notification.DeepLinkPath,
            notification.IsRead,
            notification.ReadAt,
            notification.IsSent,
            notification.SentAt,
            notification.FcmMessageId,
            notification.IsBatched,
            notification.BatchGroup,
            notification.ScheduledFor,
            notification.DateCreated);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<NotificationRuleOverride?> ResolveRuleOverrideAsync(
        Guid? familyId,
        string? referenceType,
        CancellationToken cancellationToken)
    {
        if (!familyId.HasValue || string.IsNullOrWhiteSpace(referenceType))
        {
            return null;
        }

        var rule = await _familyAdminConfigRepository.GetNotificationRuleByKeyAsync(
            familyId.Value,
            referenceType.Trim(),
            cancellationToken);

        return rule is null
            ? null
            : new NotificationRuleOverride(rule.IsEnabled, rule.PriorityOverride, rule.DeliveryDelayMinutes);
    }

    private static void EnsureOwnUser(Guid currentUserId, Guid userId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }

        if (currentUserId != userId)
        {
            throw new ForbiddenAccessException("Only the owner can access notifications.");
        }
    }
}
