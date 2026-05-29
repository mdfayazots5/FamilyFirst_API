namespace FamilyFirst.Application.DTOs.Notification;

public sealed record NotificationPreferenceDto(
    Guid PreferenceId,
    Guid UserId,
    Guid FamilyId,
    bool AttendanceAlerts,
    bool FeedbackAlerts,
    bool TaskVerificationAlerts,
    bool RewardAlerts,
    bool CalendarAlerts,
    bool WeeklyDigest,
    bool QuietHoursEnabled,
    TimeOnly QuietHoursStartTime,
    TimeOnly QuietHoursEndTime,
    TimeOnly MorningDigestTime,
    TimeOnly EveningDigestTime,
    DateTime UpdatedAt);
