namespace FamilyFirst.Application.DTOs.Task;

/// <summary>
/// Read-only family-board summary of one child (Elder home, Child family board, Elder appreciation picker).
/// Exposes only name, avatar and today's task progress — no coins, scores or profile details.
/// </summary>
public sealed record GrandchildSummaryDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    int TasksCompleted,
    int TotalTasks,
    string Status);
