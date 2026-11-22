using FamilyFirst.Application.Common.Models;
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
            .Must(value => DatabaseRegex.Matches(rules, "VerifyOtp", "PhoneNumber", value, IndianPhoneNumber.Pattern)
                && IndianPhoneNumber.IsValid(value))
            .WithMessage(IndianPhoneNumber.ValidationMessage);

        RuleFor(request => request.OtpToken)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.OtpCode)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "VerifyOtp", "OtpCode", value, @"^\d{6}$"))
            .WithMessage("OtpCode must be exactly 6 numeric digits.");
    }
}
