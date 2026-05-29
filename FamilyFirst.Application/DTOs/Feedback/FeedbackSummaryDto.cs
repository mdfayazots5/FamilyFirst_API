namespace FamilyFirst.Application.DTOs.Feedback;

public sealed record FeedbackSummaryDto(
    Guid ChildProfileId,
    int PeriodDays,
    int TotalCount,
    int AppreciationCount,
    int ComplaintCount,
    int ObservationCount,
    int HomeworkIssueCount,
    int UrgentEscalationCount,
    int WeeklySummaryCount);
