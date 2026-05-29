using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("PhoneNumber must be in E.164 format, for example +919876543210.");

        RuleFor(request => request.PhoneNumber)
            .Must(phoneNumber => !phoneNumber.StartsWith("+91", StringComparison.Ordinal) || phoneNumber.Length == 13)
            .WithMessage("Indian phone numbers must contain exactly 10 digits after +91.");

        RuleFor(request => request.FullName).SetValidator(new FullNameValidator());
        RuleFor(request => request.Role).SetValidator(new AssignableRoleValidator());
        RuleFor(request => request.LinkType).SetValidator(new LinkTypeValidator());
    }
}

public sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(request => request.Role).SetValidator(new AssignableRoleValidator());
        RuleFor(request => request.LinkType).SetValidator(new LinkTypeValidator());

        RuleFor(request => request.DisplayName)
            .MaximumLength(200)
            .When(request => !string.IsNullOrWhiteSpace(request.DisplayName));
    }
}
