using FamilyFirst.Application.DTOs.Reward;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateRewardRequestValidator : AbstractValidator<UpdateRewardRequest>
{
    public UpdateRewardRequestValidator()
    {
        RuleFor(request => request.RewardName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => request.CoinCost)
            .InclusiveBetween(10, 9999);
    }
}
