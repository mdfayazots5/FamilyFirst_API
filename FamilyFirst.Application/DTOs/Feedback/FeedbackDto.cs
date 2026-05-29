using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Feedback;

public sealed record FeedbackDto(
    Guid FeedbackId,
    Guid TeacherProfileId,
    Guid ChildProfileId,
    Guid FamilyId,
    Guid? SessionId,
    FeedbackType FeedbackType,
    FeedbackSeverity? Severity,
    string? Subject,
    string Message,
    Guid? CommentTemplateId,
    string? CommentTemplateText,
    string? WeeklySummaryJson,
    bool IsAcknowledged,
    DateTime? AcknowledgedAt,
    Guid? AcknowledgedByUserId,
    string? ParentResponseText,
    string ResolutionStatus,
    bool IsEditable,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string TeacherName,
    string ChildName);
