using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Task;

public static class TaskMetadata
{
    public static readonly IReadOnlyCollection<string> AllowedPillarTags =
        new[] { "Study", "Cleanliness", "Discipline", "ScreenControl", "Responsibility" };

    public static bool IsValidPillarTag(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || AllowedPillarTags.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string? NormalizePillarTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();

        return AllowedPillarTags.SingleOrDefault(
            pillarTag => pillarTag.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidRecurringDays(IReadOnlyCollection<int>? recurringDays, bool isRecurring)
    {
        if (!isRecurring)
        {
            return recurringDays is null || recurringDays.Count == 0 || recurringDays.All(day => day is >= 1 and <= 7);
        }

        if (recurringDays is null || recurringDays.Count == 0)
        {
            return false;
        }

        return recurringDays.All(day => day is >= 1 and <= 7)
            && recurringDays.Distinct().Count() == recurringDays.Count;
    }

    public static bool IsAllowedTaskTimeBlock(TaskTimeBlock timeBlock)
    {
        return Enum.IsDefined(timeBlock) && timeBlock != TaskTimeBlock.School;
    }
}
