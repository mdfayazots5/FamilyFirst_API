using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.FamilyName).SetValidator(new FamilyNameValidator(rules, "CreateFamily"));
        RuleFor(request => request.City).SetValidator(new CityValidator(rules, "CreateFamily"));
    }
}

public sealed class UpdateFamilyRequestValidator : AbstractValidator<UpdateFamilyRequest>
{
    public UpdateFamilyRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.FamilyName).SetValidator(new FamilyNameValidator(rules, "UpdateFamily"));
        RuleFor(request => request.City).SetValidator(new CityValidator(rules, "UpdateFamily"));
    }
}

internal sealed class FamilyNameValidator : AbstractValidator<string>
{
    public FamilyNameValidator(IRegexRuleProvider? rules, string method)
    {
        RuleFor(familyName => familyName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .Must(value => DatabaseRegex.Matches(rules, method, "FamilyName", value, @"^[\p{L}\d .'\-]+$"))
            .WithMessage("FamilyName can contain letters, digits, spaces, apostrophes, and hyphens only.");
    }
}

internal sealed class CityValidator : AbstractValidator<string?>
{
    public CityValidator(IRegexRuleProvider? rules, string method)
    {
        RuleFor(city => city)
            .MaximumLength(100)
            .Must(value => DatabaseRegex.Matches(rules, method, "City", value, @"^[\p{L} ]+$"))
            .When(city => !string.IsNullOrWhiteSpace(city))
            .WithMessage("City can contain letters and spaces only.");
    }
}
