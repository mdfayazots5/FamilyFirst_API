using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Auth;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class LoginWithPasswordRequestValidator : AbstractValidator<LoginWithPasswordRequest>
{
    public LoginWithPasswordRequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(IndianPhoneNumber.IsValid)
            .WithMessage(IndianPhoneNumber.ValidationMessage);

        RuleFor(request => request.CountryCode)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim() == IndianPhoneNumber.CountryCode)
            .WithMessage("Only Indian phone numbers (+91) are supported.");

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
