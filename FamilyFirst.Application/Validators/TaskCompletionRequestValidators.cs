using FamilyFirst.Application.DTOs.Task;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SubmitTaskCompletionRequestValidator : AbstractValidator<SubmitTaskCompletionRequest>
{
    public SubmitTaskCompletionRequestValidator()
    {
        RuleFor(request => request.ScheduledDate)
            .NotEmpty();

        RuleFor(request => request.PhotoUrl)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.PhotoUrl));
    }
}

public sealed class TaskCompletionUploadUrlRequestValidator : AbstractValidator<TaskCompletionUploadUrlRequest>
{
    public TaskCompletionUploadUrlRequestValidator()
    {
        RuleFor(request => request.TaskId)
            .NotEmpty();
    }
}
