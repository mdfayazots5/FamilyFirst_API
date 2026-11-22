using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator(IRegexRuleProvider? rules = null)
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .Must(value => DatabaseRegex.Matches(rules, "AddMember", "PhoneNumber", value, IndianPhoneNumber.Pattern)
                && IndianPhoneNumber.IsValid(value))
            .WithMessage(IndianPhoneNumber.ValidationMessage);

        RuleFor(request => request.FullName).SetValidator(new FullNameValidator(rules, "AddMember"));
        RuleFor(request => request.Role).SetValidator(new AssignableRoleValidator());
        RuleFor(request => request.LinkType).SetValidator(new LinkTypeValidator());
    }
}

public sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(request => request.Role).SetValidator(new AssignableRoleValidator());
        RuleFor(request => request.LinkType!).SetValidator(new LinkTypeValidator())
            .When(request => request.LinkType is not null);

        RuleFor(request => request.DisplayName)
            .MaximumLength(200)
            .When(request => !string.IsNullOrWhiteSpace(request.DisplayName));
    }
}
