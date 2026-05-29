using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateModuleVisibilityRequestValidator : AbstractValidator<UpdateModuleVisibilityRequest>
{
    public UpdateModuleVisibilityRequestValidator()
    {
        RuleFor(request => request.Items)
            .NotEmpty();

        RuleForEach(request => request.Items)
            .SetValidator(new ModuleVisibilityUpdateItemValidator());
    }
}

public sealed class ModuleVisibilityUpdateItemValidator : AbstractValidator<ModuleVisibilityUpdateItem>
{
    public ModuleVisibilityUpdateItemValidator()
    {
        RuleFor(item => item.Role)
            .IsInEnum()
            .Must(role => role != UserRole.SuperAdmin)
            .WithMessage("Module visibility cannot be updated for SuperAdmin.");

        RuleFor(item => item.ModuleName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
