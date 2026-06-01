using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

// TIME columns stored as DATETIME2 — only the time portion is meaningful at the application layer.
// Defaults represent: QuietStart=22:00, QuietEnd=07:00, Morning=07:00, Evening=20:00.
public sealed class NotificationPreference : BaseEntity
{
    public long UserId { get; set; }

    public long FamilyId { get; set; }

    public bool AttendanceAlerts { get; set; } = true;

    public bool FeedbackAlerts { get; set; } = true;

    public bool TaskVerificationAlerts { get; set; } = true;

    public bool RewardAlerts { get; set; } = true;

    public bool CalendarAlerts { get; set; } = true;

    public bool WeeklyDigest { get; set; } = true;

    public bool QuietHoursEnabled { get; set; } = true;

    public DateTime QuietHoursStartTime { get; set; } = new DateTime(1900, 1, 1, 22, 0, 0);

    public DateTime QuietHoursEndTime { get; set; } = new DateTime(1900, 1, 1, 7, 0, 0);

    public DateTime MorningDigestTime { get; set; } = new DateTime(1900, 1, 1, 7, 0, 0);

    public DateTime EveningDigestTime { get; set; } = new DateTime(1900, 1, 1, 20, 0, 0);

    public User? User { get; set; }

    public Family? Family { get; set; }
}
