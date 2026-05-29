using FamilyFirst.Application.DTOs.User;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    private static readonly string[] SupportedLanguages = ["en", "hi", "ta", "te", "mr"];

    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.FullName).SetValidator(new FullNameValidator());

        RuleFor(request => request.Email)
            .EmailAddress()
            .MaximumLength(300)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.ProfilePhotoUrl)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfilePhotoUrl));

        RuleFor(request => request.PreferredLanguage)
            .NotEmpty()
            .Must(language => SupportedLanguages.Contains(language))
            .WithMessage("PreferredLanguage must be one of: en, hi, ta, te, mr.");
    }
}

public sealed class FcmTokenRequestValidator : AbstractValidator<FcmTokenRequest>
{
    public FcmTokenRequestValidator()
    {
        RuleFor(request => request.FcmToken)
            .NotEmpty()
            .MaximumLength(500);
    }
}

internal sealed class FullNameValidator : AbstractValidator<string>
{
    public FullNameValidator()
    {
        RuleFor(fullName => fullName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .Matches(@"^[\p{L} .'\-]+$")
            .WithMessage("FullName can contain letters, spaces, dots, apostrophes, and hyphens only.");
    }
}

internal sealed class AssignableRoleValidator : AbstractValidator<UserRole>
{
    public AssignableRoleValidator()
    {
        RuleFor(role => role)
            .IsInEnum()
            .NotEqual(UserRole.SuperAdmin)
            .WithMessage("Role must be valid and cannot be SuperAdmin.");
    }
}

internal sealed class LinkTypeValidator : AbstractValidator<string>
{
    private static readonly string[] AllowedLinkTypes =
    [
        "Father",
        "Mother",
        "Son",
        "Daughter",
        "Grandfather",
        "Grandmother",
        "Tutor",
        "ArabicTeacher",
        "MusicTeacher",
        "Driver",
        "Caregiver",
        "Uncle",
        "Aunt"
    ];

    public LinkTypeValidator()
    {
        RuleFor(linkType => linkType)
            .NotEmpty()
            .Must(linkType => AllowedLinkTypes.Contains(linkType))
            .WithMessage("LinkType is not supported.");
    }
}
