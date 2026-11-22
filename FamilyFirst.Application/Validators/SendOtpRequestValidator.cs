using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator(IRegexRuleProvider? rules = null)
    {
        // India-only: DB rule (script 123) first, code constant as fallback, plus a hard India check.
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "SendOtp", "PhoneNumber", value, IndianPhoneNumber.Pattern)
                && IndianPhoneNumber.IsValid(value))
            .WithMessage(IndianPhoneNumber.ValidationMessage);

        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .Must(value => value?.Trim() == IndianPhoneNumber.CountryCode)
            .WithMessage("Only Indian phone numbers (+91) are supported.");
    }
}
