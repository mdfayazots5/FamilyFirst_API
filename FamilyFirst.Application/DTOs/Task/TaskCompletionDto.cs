using FamilyFirst.Domain.Enums;
using TaskCompletionStatus = FamilyFirst.Domain.Enums.TaskStatus;

namespace FamilyFirst.Application.DTOs.Task;

public sealed record TaskCompletionDto(
    Guid CompletionId,
    Guid TaskId,
    Guid ChildProfileId,
    Guid FamilyId,
    DateOnly ScheduledDate,
    string TaskName,
    string ChildName,
    TaskCompletionStatus Status,
    string? PhotoUrl,
    DateTime? SubmittedAt,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ReviewNote,
    int CoinsAwarded);

public sealed record BatchApproveResultDto(int ApprovedCount);
