namespace FamilyFirst.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid PreferenceId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    public bool AttendanceAlerts { get; set; } = true;

    public bool FeedbackAlerts { get; set; } = true;

    public bool TaskVerificationAlerts { get; set; } = true;

    public bool RewardAlerts { get; set; } = true;

    public bool CalendarAlerts { get; set; } = true;

    public bool WeeklyDigest { get; set; } = true;

    public bool QuietHoursEnabled { get; set; } = true;

    public TimeOnly QuietHoursStartTime { get; set; } = new(22, 0);

    public TimeOnly QuietHoursEndTime { get; set; } = new(7, 0);

    public TimeOnly MorningDigestTime { get; set; } = new(7, 0);

    public TimeOnly EveningDigestTime { get; set; } = new(20, 0);

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Family? Family { get; set; }
}
