using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class JoinFamilyRequestValidator : AbstractValidator<JoinFamilyRequest>
{
    public JoinFamilyRequestValidator()
    {
        RuleFor(request => request.JoinCode)
            .NotEmpty()
            .Matches(@"^[A-Za-z0-9]{6}$")
            .WithMessage("JoinCode must be exactly 6 alphanumeric characters.");

        RuleFor(request => request.FullName).SetValidator(new FullNameValidator());
        RuleFor(request => request.Role).SetValidator(new AssignableRoleValidator());
        RuleFor(request => request.LinkType).SetValidator(new LinkTypeValidator());
    }
}
