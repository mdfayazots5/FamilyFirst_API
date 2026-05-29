using FamilyFirst.Application.DTOs.Task;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class DeductCoinsRequestValidator : AbstractValidator<DeductCoinsRequest>
{
    public DeductCoinsRequestValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(100000);

        RuleFor(request => request.Note)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(500);
    }
}
