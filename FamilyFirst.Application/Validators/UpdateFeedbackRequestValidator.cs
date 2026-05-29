using FamilyFirst.Application.DTOs.Feedback;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateFeedbackRequestValidator : AbstractValidator<UpdateFeedbackRequest>
{
    public UpdateFeedbackRequestValidator()
    {
        RuleFor(request => request.Message)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(2000);

        RuleFor(request => request.Severity)
            .Must(severity => !severity.HasValue || Enum.IsDefined(severity.Value))
            .WithMessage("Severity must be a valid value when provided.");
    }
}
