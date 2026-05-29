using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class ReviewTaskCompletionRequestValidator : AbstractValidator<ReviewTaskCompletionRequest>
{
    public ReviewTaskCompletionRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => status is TaskStatus.Approved or TaskStatus.Flagged)
            .WithMessage("Status must be Approved or Flagged.");

        RuleFor(request => request.ReviewNote)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(500)
            .When(request => request.Status == TaskStatus.Flagged);

        RuleFor(request => request.ReviewNote)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.ReviewNote));
    }
}
