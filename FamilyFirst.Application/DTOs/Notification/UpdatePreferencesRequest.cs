namespace FamilyFirst.Application.DTOs.Notification;

public sealed class UpdatePreferencesRequest
{
    public bool AttendanceAlerts { get; init; } = true;

    public bool FeedbackAlerts { get; init; } = true;

    public bool TaskVerificationAlerts { get; init; } = true;

    public bool RewardAlerts { get; init; } = true;

    public bool CalendarAlerts { get; init; } = true;

    public bool WeeklyDigest { get; init; } = true;

    public bool QuietHoursEnabled { get; init; } = true;

    public TimeOnly QuietHoursStartTime { get; init; } = new(22, 0);

    public TimeOnly QuietHoursEndTime { get; init; } = new(7, 0);

    public TimeOnly MorningDigestTime { get; init; } = new(7, 0);

    public TimeOnly EveningDigestTime { get; init; } = new(20, 0);
}
