using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "VerifyOtp", "PhoneNumber", value, @"^\+[1-9]\d{7,14}$"))
            .WithMessage("PhoneNumber must be in E.164 format, for example +919876543210.");

        RuleFor(request => request.OtpToken)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.OtpCode)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "VerifyOtp", "OtpCode", value, @"^\d{6}$"))
            .WithMessage("OtpCode must be exactly 6 numeric digits.");
    }
}
