using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Feedback;

public sealed class SubmitFeedbackRequest
{
    public Guid ChildProfileId { get; init; }

    public FeedbackType FeedbackType { get; init; }

    public FeedbackSeverity? Severity { get; init; }

    public string? Subject { get; init; }

    public string Message { get; init; } = string.Empty;

    public Guid? CommentTemplateId { get; init; }

    public Guid? SessionId { get; init; }

    public string? WeeklySummaryJson { get; init; }
}
