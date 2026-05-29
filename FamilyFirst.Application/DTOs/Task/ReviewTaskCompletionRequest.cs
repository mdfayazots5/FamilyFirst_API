using FamilyFirst.Domain.Enums;
using TaskCompletionStatus = FamilyFirst.Domain.Enums.TaskStatus;

namespace FamilyFirst.Application.DTOs.Task;

public sealed class ReviewTaskCompletionRequest
{
    public TaskCompletionStatus Status { get; init; }

    public string? ReviewNote { get; init; }
}
