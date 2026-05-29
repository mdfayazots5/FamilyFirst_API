namespace FamilyFirst.Application.DTOs.Attendance;

public sealed record CommentTemplateDto(
    Guid TemplateId,
    Guid? FamilyId,
    string TemplateText,
    string Category,
    bool IsSystem,
    int SortOrder);

public static class CommentTemplateCategories
{
    public const string Attendance = "Attendance";
    public const string Feedback = "Feedback";
    public const string Homework = "Homework";

    public static readonly IReadOnlyCollection<string> AllowedValues =
        new[] { Attendance, Feedback, Homework };

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Equals(Attendance, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Attendance;
            return true;
        }

        if (trimmedValue.Equals(Feedback, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Feedback;
            return true;
        }

        if (trimmedValue.Equals(Homework, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Homework;
            return true;
        }

        return false;
    }
}
