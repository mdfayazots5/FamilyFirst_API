namespace FamilyFirst.Application.DTOs.Attendance;

public sealed class CreateCommentTemplateRequest
{
    public string TemplateText { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;
}
