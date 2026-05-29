using System.Text.Json;
using FamilyFirst.Application.DTOs.Feedback;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SubmitFeedbackRequestValidator : AbstractValidator<SubmitFeedbackRequest>
{
    public SubmitFeedbackRequestValidator()
    {
        RuleFor(request => request.ChildProfileId)
            .NotEmpty();

        RuleFor(request => request.FeedbackType)
            .IsInEnum();

        RuleFor(request => request.Severity)
            .Must((request, severity) => FeedbackValidationRules.IsValidSeverity(request.FeedbackType, severity))
            .WithMessage("Severity is required for Complaint and UrgentEscalation feedback and must be a valid value.");

        RuleFor(request => request.Subject)
            .MaximumLength(300)
            .When(request => !string.IsNullOrWhiteSpace(request.Subject));

        RuleFor(request => request.Message)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(2000);

        RuleFor(request => request.CommentTemplateId)
            .Must(templateId => !templateId.HasValue || templateId.Value != Guid.Empty)
            .WithMessage("CommentTemplateId must be a valid value when provided.");

        RuleFor(request => request.SessionId)
            .Must(sessionId => !sessionId.HasValue || sessionId.Value != Guid.Empty)
            .WithMessage("SessionId must be a valid value when provided.");

        RuleFor(request => request.WeeklySummaryJson)
            .Must((request, weeklySummaryJson) => FeedbackValidationRules.IsValidWeeklySummary(request.FeedbackType, weeklySummaryJson))
            .WithMessage("WeeklySummaryJson is required for WeeklySummary feedback and must include attendanceRate, homeworkRate, standoutMoment, and focusArea.");
    }
}

internal static class FeedbackValidationRules
{
    public static bool IsValidSeverity(FeedbackType feedbackType, FeedbackSeverity? severity)
    {
        if (feedbackType is FeedbackType.Complaint or FeedbackType.UrgentEscalation)
        {
            return severity.HasValue && Enum.IsDefined(severity.Value);
        }

        return !severity.HasValue || Enum.IsDefined(severity.Value);
    }

    public static bool IsValidWeeklySummary(FeedbackType feedbackType, string? weeklySummaryJson)
    {
        if (feedbackType != FeedbackType.WeeklySummary)
        {
            return string.IsNullOrWhiteSpace(weeklySummaryJson) || IsValidWeeklySummaryPayload(weeklySummaryJson);
        }

        return IsValidWeeklySummaryPayload(weeklySummaryJson);
    }

    private static bool IsValidWeeklySummaryPayload(string? weeklySummaryJson)
    {
        if (string.IsNullOrWhiteSpace(weeklySummaryJson))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<WeeklySummaryPayload>(weeklySummaryJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return payload is not null
                && payload.AttendanceRate is >= 0 and <= 100
                && payload.HomeworkRate is >= 0 and <= 100
                && !string.IsNullOrWhiteSpace(payload.StandoutMoment)
                && !string.IsNullOrWhiteSpace(payload.FocusArea);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class WeeklySummaryPayload
    {
        public int AttendanceRate { get; init; }

        public int HomeworkRate { get; init; }

        public string? StandoutMoment { get; init; }

        public string? FocusArea { get; init; }
    }
}
