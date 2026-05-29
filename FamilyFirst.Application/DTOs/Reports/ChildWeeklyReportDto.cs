namespace FamilyFirst.Application.DTOs.Reports;

public sealed record ChildWeeklyReportDto(
    Guid ChildProfileId,
    string ChildName,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    decimal AttendanceRate,
    decimal TaskRate,
    FeedbackSummaryDto Feedback,
    IReadOnlyCollection<PillarScoreDto> PillarScores);

public sealed record PillarScoreDto(
    string Pillar,
    int Score);
