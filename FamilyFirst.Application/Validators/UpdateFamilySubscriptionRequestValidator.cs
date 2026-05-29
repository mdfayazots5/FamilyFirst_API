using FamilyFirst.Application.DTOs.Admin;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateFamilySubscriptionRequestValidator : AbstractValidator<UpdateFamilySubscriptionRequest>
{
    public UpdateFamilySubscriptionRequestValidator()
    {
        RuleFor(request => request.PlanId)
            .GreaterThan(0);

        RuleFor(request => request.ExtendTrialDays)
            .GreaterThan(0)
            .When(request => request.ExtendTrialDays.HasValue);

        RuleFor(request => request.Status)
            .MaximumLength(20)
            .When(request => !string.IsNullOrWhiteSpace(request.Status));
    }
}
