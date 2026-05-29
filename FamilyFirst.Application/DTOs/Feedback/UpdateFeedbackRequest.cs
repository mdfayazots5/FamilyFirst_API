using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Feedback;

public sealed class UpdateFeedbackRequest
{
    public string Message { get; init; } = string.Empty;

    public FeedbackSeverity? Severity { get; init; }
}
