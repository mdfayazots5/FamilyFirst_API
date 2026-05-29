using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class AttendanceSession : BaseEntity
{
    public Guid TeacherProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public string SessionName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string? BatchName { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public bool IsSubmitted { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public bool IsRecurring { get; set; }

    public string? RecurringDays { get; set; }

    public bool IsActive { get; set; } = true;

    public TeacherProfile? TeacherProfile { get; set; }

    public Family? Family { get; set; }
}
