namespace FamilyFirst.Application.DTOs.Attendance;

public sealed record AttendanceSummaryDto(
    Guid SessionId,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int LeftEarlyCount);
