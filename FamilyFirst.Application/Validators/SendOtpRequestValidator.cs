using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("PhoneNumber must be in E.164 format, for example +919876543210.");

        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .MaximumLength(5)
            .Matches(@"^\+[1-9]\d{0,4}$");

        RuleFor(request => request.PhoneNumber)
            .Must(phoneNumber => !phoneNumber.StartsWith("+91", StringComparison.Ordinal) || phoneNumber.Length == 13)
            .WithMessage("Indian phone numbers must contain exactly 10 digits after +91.");
    }
}
