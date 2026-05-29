using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(request => request.FamilyName).SetValidator(new FamilyNameValidator());
        RuleFor(request => request.City).SetValidator(new CityValidator());
    }
}

public sealed class UpdateFamilyRequestValidator : AbstractValidator<UpdateFamilyRequest>
{
    public UpdateFamilyRequestValidator()
    {
        RuleFor(request => request.FamilyName).SetValidator(new FamilyNameValidator());
        RuleFor(request => request.City).SetValidator(new CityValidator());
    }
}

internal sealed class FamilyNameValidator : AbstractValidator<string>
{
    public FamilyNameValidator()
    {
        RuleFor(familyName => familyName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .Matches(@"^[\p{L}\d .'\-]+$")
            .WithMessage("FamilyName can contain letters, digits, spaces, apostrophes, and hyphens only.");
    }
}

internal sealed class CityValidator : AbstractValidator<string?>
{
    public CityValidator()
    {
        RuleFor(city => city)
            .MaximumLength(100)
            .Matches(@"^[\p{L} ]+$")
            .When(city => !string.IsNullOrWhiteSpace(city))
            .WithMessage("City can contain letters and spaces only.");
    }
}
