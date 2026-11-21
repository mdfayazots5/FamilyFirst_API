using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SetPinRequestValidator : AbstractValidator<SetPinRequest>
{
    public SetPinRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.Pin)
            .NotEmpty()
            .Must(pin => PinValidator.IsValid(rules, "SetPin", pin))
            .WithMessage("PIN must be exactly 4 numeric digits and cannot use repeated digits.");
    }
}

public sealed class VerifyPinRequestValidator : AbstractValidator<VerifyPinRequest>
{
    public VerifyPinRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.Pin)
            .NotEmpty()
            .Must(pin => PinValidator.IsValid(rules, "VerifyPin", pin))
            .WithMessage("PIN must be exactly 4 numeric digits and cannot use repeated digits.");
    }
}

internal static class PinValidator
{
    public static bool IsValid(IRegexRuleProvider? rules, string method, string pin)
    {
        return pin is not null
            && DatabaseRegex.Matches(rules, method, "Pin", pin, @"\A\d{4}\z")
            && pin.Distinct().Count() > 1;
    }
}
