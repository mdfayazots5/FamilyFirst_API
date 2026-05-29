using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _notificationPreferenceRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public NotificationPreferenceService(
        INotificationPreferenceRepository notificationPreferenceRepository,
        IFamilyMemberRepository familyMemberRepository)
    {
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid currentUserId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);

        return ToDto(preference);
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid currentUserId,
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        preference.AttendanceAlerts = request.AttendanceAlerts;
        preference.FeedbackAlerts = request.FeedbackAlerts;
        preference.TaskVerificationAlerts = request.TaskVerificationAlerts;
        preference.RewardAlerts = request.RewardAlerts;
        preference.CalendarAlerts = request.CalendarAlerts;
        preference.WeeklyDigest = request.WeeklyDigest;
        preference.QuietHoursEnabled = request.QuietHoursEnabled;
        preference.QuietHoursStartTime = request.QuietHoursStartTime;
        preference.QuietHoursEndTime = request.QuietHoursEndTime;
        preference.MorningDigestTime = request.MorningDigestTime;
        preference.EveningDigestTime = request.EveningDigestTime;
        preference.UpdatedAt = DateTime.UtcNow;

        await _notificationPreferenceRepository.UpdateAsync(preference, cancellationToken);

        return ToDto(preference);
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

        var membership = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(userId, cancellationToken)
            ?? throw new ForbiddenAccessException("Notification preferences require an active family membership.");

        var preference = new NotificationPreference
        {
            UserId = userId,
            FamilyId = membership.FamilyId
        };

        await _notificationPreferenceRepository.AddAsync(preference, cancellationToken);

        return preference;
    }

    private static void EnsureOwnUser(Guid currentUserId, Guid userId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }

        if (currentUserId != userId)
        {
            throw new ForbiddenAccessException("Only the owner can access notification preferences.");
        }
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference preference)
    {
        return new NotificationPreferenceDto(
            preference.PreferenceId,
            preference.UserId,
            preference.FamilyId,
            preference.AttendanceAlerts,
            preference.FeedbackAlerts,
            preference.TaskVerificationAlerts,
            preference.RewardAlerts,
            preference.CalendarAlerts,
            preference.WeeklyDigest,
            preference.QuietHoursEnabled,
            preference.QuietHoursStartTime,
            preference.QuietHoursEndTime,
            preference.MorningDigestTime,
            preference.EveningDigestTime,
            preference.UpdatedAt);
    }
}
