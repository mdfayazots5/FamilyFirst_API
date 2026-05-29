using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class ReviewRedemptionRequestValidator : AbstractValidator<ReviewRedemptionRequest>
{
    public ReviewRedemptionRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => status is RedemptionStatus.Approved or RedemptionStatus.Rejected)
            .WithMessage("Status must be Approved or Rejected.");

        RuleFor(request => request.ParentNote)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.ParentNote));
    }
}
