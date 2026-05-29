using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SetPinRequestValidator : AbstractValidator<SetPinRequest>
{
    public SetPinRequestValidator()
    {
        RuleFor(request => request.Pin)
            .NotEmpty()
            .Must(BeValidPin)
            .WithMessage("PIN must be exactly 4 numeric digits and cannot use repeated digits.");
    }

    private static bool BeValidPin(string pin)
    {
        return PinValidator.IsValid(pin);
    }
}

public sealed class VerifyPinRequestValidator : AbstractValidator<VerifyPinRequest>
{
    public VerifyPinRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.Pin)
            .NotEmpty()
            .Must(PinValidator.IsValid)
            .WithMessage("PIN must be exactly 4 numeric digits and cannot use repeated digits.");
    }
}

internal static class PinValidator
{
    public static bool IsValid(string pin)
    {
        return pin is not null
            && pin.Length == 4
            && pin.All(char.IsDigit)
            && pin.Distinct().Count() > 1;
    }
}
