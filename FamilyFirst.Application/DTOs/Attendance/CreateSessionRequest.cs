namespace FamilyFirst.Application.DTOs.Attendance;

public sealed class CreateSessionRequest
{
    public string SessionName { get; init; } = string.Empty;

    public string SubjectName { get; init; } = string.Empty;

    public string? BatchName { get; init; }

    public DateOnly? ScheduledDate { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    public bool IsRecurring { get; init; }

    public int[]? RecurringDays { get; init; }
}
