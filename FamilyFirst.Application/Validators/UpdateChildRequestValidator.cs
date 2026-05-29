using FamilyFirst.Application.DTOs.Family;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateChildRequestValidator : AbstractValidator<UpdateChildRequest>
{
    public UpdateChildRequestValidator()
    {
        RuleFor(request => request.DateOfBirth).SetValidator(new ChildDateOfBirthValidator());
        RuleFor(request => request.GradeLevel).MaximumLength(50);
        RuleFor(request => request.SchoolName).MaximumLength(200);
        RuleFor(request => request.AvatarCode).SetValidator(new AvatarCodeValidator());
    }
}
