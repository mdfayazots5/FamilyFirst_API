using FluentValidation;

namespace FamilyFirst.Application.Validators;

internal sealed class AvatarCodeValidator : AbstractValidator<string>
{
    private static readonly IReadOnlySet<string> AllowedAvatarCodes = Enumerable.Range(1, 10)
        .Select(index => $"avatar_{index:00}")
        .ToHashSet(StringComparer.Ordinal);

    public AvatarCodeValidator()
    {
        RuleFor(avatarCode => avatarCode)
            .NotEmpty()
            .Must(avatarCode => AllowedAvatarCodes.Contains(avatarCode))
            .WithMessage("AvatarCode must be avatar_01 through avatar_10.");
    }
}

internal sealed class ChildDateOfBirthValidator : AbstractValidator<DateOnly?>
{
    public ChildDateOfBirthValidator()
    {
        RuleFor(dateOfBirth => dateOfBirth)
            .Must(BeAllowedChildAge)
            .WithMessage("Child age must be between 3 and 17 years.");
    }

    private static bool BeAllowedChildAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ageYears = CalculateAgeYears(dateOfBirth.Value, today);

        return ageYears is >= 3 and <= 17;
    }

    private static int CalculateAgeYears(DateOnly dateOfBirth, DateOnly today)
    {
        var ageYears = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-ageYears))
        {
            ageYears--;
        }

        return ageYears;
    }
}
