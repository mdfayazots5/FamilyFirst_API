namespace FamilyFirst.Application.DTOs.Attendance;

public sealed class UpdateCommentTemplateRequest
{
    public string TemplateText { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;
}
