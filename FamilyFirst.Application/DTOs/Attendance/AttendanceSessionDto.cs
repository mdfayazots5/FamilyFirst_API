namespace FamilyFirst.Application.DTOs.Attendance;

public sealed record AttendanceSessionDto(
    Guid SessionId,
    Guid TeacherProfileId,
    Guid FamilyId,
    string TeacherName,
    string SessionName,
    string SubjectName,
    string? BatchName,
    DateOnly ScheduledDate,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsSubmitted,
    DateTime? SubmittedAt,
    bool IsRecurring,
    IReadOnlyCollection<int> RecurringDays,
    bool IsActive);
