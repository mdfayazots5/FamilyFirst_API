namespace FamilyFirst.Application.DTOs.Reports;

public sealed record FeedbackSummaryDto(
    int TotalCount,
    int AppreciationCount,
    int ComplaintCount,
    int ObservationCount,
    int HomeworkIssueCount,
    int UrgentEscalationCount,
    int WeeklySummaryCount,
    string? LatestParentRemark);
