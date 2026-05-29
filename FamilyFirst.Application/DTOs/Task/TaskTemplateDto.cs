using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Task;

public sealed record TaskTemplateDto(
    Guid TemplateId,
    string TaskName,
    string? Instructions,
    string? IconCode,
    TaskTimeBlock TimeBlock,
    int DurationMinutes,
    int CoinValue,
    bool IsPhotoRequired,
    string? PillarTag,
    bool IsRecurring,
    IReadOnlyCollection<int> RecurringDays,
    DateOnly ActiveFromDate,
    string Category,
    string? AgeGroup);

public sealed class CreateTaskTemplateRequest
{
    public string TaskName { get; init; } = string.Empty;

    public string? Instructions { get; init; }

    public string? IconCode { get; init; }

    public TaskTimeBlock TimeBlock { get; init; }

    public int DurationMinutes { get; init; }

    public int CoinValue { get; init; }

    public bool IsPhotoRequired { get; init; }

    public string? PillarTag { get; init; }

    public bool IsRecurring { get; init; } = true;

    public IReadOnlyCollection<int>? RecurringDays { get; init; }

    public DateOnly ActiveFromDate { get; init; }

    public string Category { get; init; } = string.Empty;

    public string? AgeGroup { get; init; }
}
