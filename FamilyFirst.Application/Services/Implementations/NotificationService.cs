using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

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
    private readonly IApiLogService _apiLogService;
    private readonly IErrorCodeService _errorCodeService;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPreferenceService notificationPreferenceService,
        IUserRepository userRepository,
        IFamilyAdminConfigRepository familyAdminConfigRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        IPushNotificationService pushNotificationService,
        IApiLogService apiLogService,
        IErrorCodeService errorCodeService)
    {
        _notificationRepository = notificationRepository;
        _notificationPreferenceService = notificationPreferenceService;
        _userRepository = userRepository;
        _familyAdminConfigRepository = familyAdminConfigRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _pushNotificationService = pushNotificationService;
        _apiLogService = apiLogService;
        _errorCodeService = errorCodeService;
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

        LogApiCall(nameof(CreateManyAsync), new { Count = requests.Count }, new { Count = notifications.Count });
        return notifications;
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var recipient = await _userRepository.GetByIdAsync(request.RecipientUserId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
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
            FamilyId = null,
            RecipientUserId = recipient.InternalId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Priority = request.Priority,
            Channel = request.Channel,
            ReferenceType = NormalizeOptional(request.ReferenceType),
            ReferenceId = null,
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

            var suppressedResponse = ToDto(notification);
            LogApiCall(nameof(CreateAsync), new { request.RecipientUserId, request.FamilyId, request.ReferenceType, request.Priority }, new { suppressedResponse.NotificationId, suppressedResponse.IsSent });
            return suppressedResponse;
        }

        if (notification.Priority == NotificationPriority.Urgent
            || (notification.Priority == NotificationPriority.High
                && !notification.IsBatched
                && !notification.ScheduledFor.HasValue))
        {
            await TrySendInlineAsync(notification, recipient, cancellationToken);
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }

        var response = ToDto(notification);
        LogApiCall(nameof(CreateAsync), new { request.RecipientUserId, request.FamilyId, request.ReferenceType, request.Priority }, new { response.NotificationId, response.IsSent });
        return response;
    }

    public async Task<PaginatedList<NotificationDto>> ListNotificationsAsync(
        Guid currentUserId,
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isRead,
        CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);

        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };
        var notifications = await _notificationRepository.ListByRecipientAsync(userId, isRead, cancellationToken);

        var response = PaginatedList<NotificationDto>.Create(
            notifications
                .OrderByDescending(notification => notification.DateCreated)
                .Select(ToDto),
            normalizedPageNumber,
            normalizedPageSize);
        LogApiCall(nameof(ListNotificationsAsync), new { currentUserId, userId, pageNumber, pageSize, isRead }, new { response.TotalCount });
        return response;
    }

    public async Task<bool> MarkReadAsync(
        Guid currentUserId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);

        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (notification.RecipientUser?.Id != userId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        LogApiCall(nameof(MarkReadAsync), new { currentUserId, userId, notificationId }, new { Success = true });
        return true;
    }

    public async Task<MarkAllReadResultDto> MarkAllReadAsync(
        Guid currentUserId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);
        var count = await _notificationRepository.MarkAllReadAsync(userId, cancellationToken);

        var response = new MarkAllReadResultDto(count);
        LogApiCall(nameof(MarkAllReadAsync), new { currentUserId, userId }, new { response.Count });
        return response;
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(currentChildProfileId.Value, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        var childName = childProfile.User?.FullName
            ?? childProfile.FamilyMember?.User?.FullName
            ?? "Child";
        var currentTaskName = NormalizeOptional(request.CurrentTaskName);
        var title = currentTaskName is null ? "Emergency alert" : "Task help request";
        var body = currentTaskName is null
            ? $"{childName} pressed Emergency. Check on them now."
            : $"{childName} needs help with task: {currentTaskName}.";
        var familyMembers = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var recipients = familyMembers
            .Where(familyMember => familyMember.Role is UserRole.Parent or UserRole.FamilyAdmin)
            .Select(familyMember => familyMember.UserId)
            .Distinct()
            .ToArray();
        var deepLinkPath = $"/families/{familyId}";

        var response = await CreateManyAsync(
            recipients
                .Select(recipientUserId => new CreateNotificationRequest
                {
                    FamilyId = familyId,
                    RecipientUserId = familyMembers
                        .First(familyMember => familyMember.UserId == recipientUserId)
                        .User?.Id ?? Guid.Empty,
                    Title = title,
                    Body = body,
                    Priority = NotificationPriority.Urgent,
                    ReferenceType = "System",
                    ReferenceId = childProfile.Id,
                    DeepLinkPath = deepLinkPath
                })
                .ToArray(),
            cancellationToken);

        LogApiCall(nameof(SendEmergencyAsync), new { currentUserId, familyId, currentChildProfileId, request.CurrentTaskName }, new { Count = response.Count });
        return response;
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

        var deliveryDelayMinutes = ruleOverride.DeliveryDelayMinutes.GetValueOrDefault();
        if (deliveryDelayMinutes > 0)
        {
            notification.ScheduledFor = utcNow.AddMinutes(deliveryDelayMinutes);
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
            null,
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

    private async Task EnsureOwnUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }

        if (currentUserId != userId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }
}
