using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Task;

public sealed record TaskItemDto(
    Guid TaskId,
    Guid FamilyId,
    Guid? ChildProfileId,
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
    DateOnly? ActiveToDate,
    bool IsActive);
