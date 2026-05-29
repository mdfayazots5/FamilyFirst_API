using FamilyFirst.Application.DTOs.Feedback;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class AcknowledgeRequestValidator : AbstractValidator<AcknowledgeRequest>
{
    public AcknowledgeRequestValidator()
    {
        RuleFor(request => request.ParentResponseText)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.ParentResponseText));
    }
}
