using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class TeacherFeedback : BaseEntity
{
    public Guid TeacherProfileId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public Guid? SessionId { get; set; }

    public FeedbackType FeedbackType { get; set; }

    public FeedbackSeverity? Severity { get; set; }

    public string? Subject { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? CommentTemplateId { get; set; }

    public string? WeeklySummaryJson { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public Guid? AcknowledgedByUserId { get; set; }

    public string? ParentResponseText { get; set; }

    public string ResolutionStatus { get; set; } = "Open";

    public bool IsEditable { get; set; }

    public TeacherProfile? TeacherProfile { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public AttendanceSession? Session { get; set; }

    public CommentTemplate? CommentTemplate { get; set; }

    public User? AcknowledgedByUser { get; set; }
}
