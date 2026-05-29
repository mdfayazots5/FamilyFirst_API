namespace FamilyFirst.Application.DTOs.Reports;

public sealed record WeeklyDigestDto(
    Guid FamilyId,
    string FamilyName,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    int FamilyScore,
    string FamilyScoreTrend,
    int TotalFeedbackCount,
    IReadOnlyCollection<WeeklyDigestChildDto> Children,
    IReadOnlyCollection<WeeklyDigestUpcomingEventDto> UpcomingEvents);

public sealed record WeeklyDigestChildDto(
    Guid ChildProfileId,
    string ChildName,
    decimal AttendanceRate,
    decimal TaskRate,
    int FeedbackCount);

public sealed record WeeklyDigestUpcomingEventDto(
    Guid EventId,
    string EventTitle,
    DateTime StartDateTime,
    DateTime? EndDateTime,
    string EventType,
    Guid? LinkedChildProfileId);
