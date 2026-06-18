using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _notificationPreferenceRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;

    public NotificationPreferenceService(
        INotificationPreferenceRepository notificationPreferenceRepository,
        IFamilyMemberRepository familyMemberRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService)
    {
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _familyMemberRepository = familyMemberRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
    }

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid currentUserId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);

        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        var response = ToDto(preference);
        LogApiCall(nameof(GetPreferencesAsync), new { currentUserId, userId }, new { response.PreferenceId });
        return response;
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid currentUserId,
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);
        var membership = await GetMembershipOrThrowAsync(userId, cancellationToken);
        await EnsurePermissionAsync(membership.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        preference.AttendanceAlerts = request.AttendanceAlerts;
        preference.FeedbackAlerts = request.FeedbackAlerts;
        preference.TaskVerificationAlerts = request.TaskVerificationAlerts;
        preference.RewardAlerts = request.RewardAlerts;
        preference.CalendarAlerts = request.CalendarAlerts;
        preference.WeeklyDigest = request.WeeklyDigest;
        preference.QuietHoursEnabled = request.QuietHoursEnabled;
        preference.QuietHoursStartTime = new DateTime(1900, 1, 1).Add(request.QuietHoursStartTime.ToTimeSpan());
        preference.QuietHoursEndTime = new DateTime(1900, 1, 1).Add(request.QuietHoursEndTime.ToTimeSpan());
        preference.MorningDigestTime = new DateTime(1900, 1, 1).Add(request.MorningDigestTime.ToTimeSpan());
        preference.EveningDigestTime = new DateTime(1900, 1, 1).Add(request.EveningDigestTime.ToTimeSpan());
        preference.LastUpdated = DateTime.UtcNow;

        await _notificationPreferenceRepository.UpdateAsync(preference, cancellationToken);

        var response = ToDto(preference);
        LogApiCall(nameof(UpdatePreferencesAsync), new { currentUserId, userId }, new { response.PreferenceId });
        return response;
    }

    public async Task<NotificationPreference> GetOrCreatePreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existingPreference = await _notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existingPreference is not null)
        {
            return existingPreference;
        }

        var membership = await GetMembershipOrThrowAsync(userId, cancellationToken);
        var preference = new NotificationPreference
        {
            UserId = membership.UserId,
            FamilyId = membership.FamilyId
        };

        await _notificationPreferenceRepository.AddAsync(preference, cancellationToken);

        return preference;
    }

    private async Task<FamilyMember> GetMembershipOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(userId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Notifications,
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

    private static NotificationPreferenceDto ToDto(NotificationPreference preference)
    {
        return new NotificationPreferenceDto(
            preference.Id,
            preference.User?.Id ?? Guid.Empty,
            preference.Family?.Id ?? Guid.Empty,
            preference.AttendanceAlerts,
            preference.FeedbackAlerts,
            preference.TaskVerificationAlerts,
            preference.RewardAlerts,
            preference.CalendarAlerts,
            preference.WeeklyDigest,
            preference.QuietHoursEnabled,
            TimeOnly.FromDateTime(preference.QuietHoursStartTime),
            TimeOnly.FromDateTime(preference.QuietHoursEndTime),
            TimeOnly.FromDateTime(preference.MorningDigestTime),
            TimeOnly.FromDateTime(preference.EveningDigestTime),
            preference.LastUpdated ?? preference.DateCreated);
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }
}
