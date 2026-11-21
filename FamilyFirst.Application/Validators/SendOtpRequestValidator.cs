using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "SendOtp", "PhoneNumber", value, @"^\+[1-9]\d{7,14}$"))
            .WithMessage("PhoneNumber must be in E.164 format, for example +919876543210.");

        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .MaximumLength(5)
            .Must(value => DatabaseRegex.Matches(rules, "SendOtp", "CountryCode", value, @"^\+[1-9]\d{0,4}$"))
            .WithMessage("'{PropertyName}' is not in the correct format.");

        RuleFor(request => request.PhoneNumber)
            .Must(phoneNumber => phoneNumber is null || !phoneNumber.StartsWith("+91", StringComparison.Ordinal) || phoneNumber.Length == 13)
            .WithMessage("Indian phone numbers must contain exactly 10 digits after +91.");
    }
}
