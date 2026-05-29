namespace FamilyFirst.Application.DTOs.Task;

public sealed class SubmitTaskCompletionRequest
{
    public DateOnly ScheduledDate { get; init; }

    public string? PhotoUrl { get; init; }
}
