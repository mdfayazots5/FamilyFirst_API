using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Application.Services.Implementations;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateRewardRequestValidator : AbstractValidator<CreateRewardRequest>
{
    public CreateRewardRequestValidator()
    {
        RuleFor(request => request.RewardName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.Description));

        RuleFor(request => request.IconCode)
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(request.IconCode));

        RuleFor(request => request.Category)
            .Must(category => RewardCatalog.NormalizeCategory(category) is not null)
            .WithMessage($"Category must be one of: {string.Join(", ", RewardCatalog.AllowedCategories)}.");

        RuleFor(request => request.CoinCost)
            .InclusiveBetween(10, 9999);
    }
}
