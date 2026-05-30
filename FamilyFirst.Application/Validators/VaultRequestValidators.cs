using FamilyFirst.Application.DTOs.Vault;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateVaultDocumentRequestValidator : AbstractValidator<CreateVaultDocumentRequest>
{
    public CreateVaultDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentName)
            .NotEmpty().WithMessage("Document name is required.")
            .MaximumLength(500).WithMessage("Document name must not exceed 500 characters.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("MemberId is required.");

        RuleFor(x => x.Category)
            .InclusiveBetween(1, 8).WithMessage("Category must be between 1 and 8.");

        RuleFor(x => x.FileUrl)
            .NotEmpty().WithMessage("FileUrl is required.")
            .MaximumLength(1000).WithMessage("FileUrl must not exceed 1000 characters.");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 20)
            .WithMessage("Maximum 20 tags allowed.")
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must not exceed 50 characters.");

        RuleFor(x => x.Visibility)
            .Must(v => v == null || (v >= 1 && v <= 4))
            .WithMessage("Visibility must be between 1 and 4.");
    }
}

public sealed class UpdateVaultDocumentRequestValidator : AbstractValidator<UpdateVaultDocumentRequest>
{
    public UpdateVaultDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentName)
            .MaximumLength(500).WithMessage("Document name must not exceed 500 characters.")
            .When(x => x.DocumentName != null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 20)
            .WithMessage("Maximum 20 tags allowed.")
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must not exceed 50 characters.");

        RuleFor(x => x.Visibility)
            .Must(v => v == null || (v >= 1 && v <= 4))
            .WithMessage("Visibility must be between 1 and 4.");

        RuleFor(x => x.NewFileUrl)
            .MaximumLength(1000).WithMessage("FileUrl must not exceed 1000 characters.")
            .When(x => x.NewFileUrl != null);
    }
}

public sealed class VaultUploadUrlRequestValidator : AbstractValidator<VaultUploadUrlRequest>
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/heic",
        "image/heif"
    };

    public VaultUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.")
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage("ContentType must be one of: application/pdf, image/jpeg, image/png, image/heic, image/heif.");

        RuleFor(x => x.Category)
            .InclusiveBetween(1, 8).WithMessage("Category must be between 1 and 8.");
    }
}

public sealed class UpdateVaultFamilySettingsRequestValidator : AbstractValidator<UpdateVaultFamilySettingsRequest>
{
    public UpdateVaultFamilySettingsRequestValidator()
    {
        RuleFor(x => x.EmergencyAccessMode)
            .InclusiveBetween(1, 3)
            .WithMessage("EmergencyAccessMode must be 1 (LoginRequired), 2 (PinOnly), or 3 (NoLogin).");

        RuleFor(x => x.EmergencyPin)
            .Must(pin => pin == null || (pin.Length == 4 && pin.All(char.IsDigit)))
            .WithMessage("Emergency PIN must be exactly 4 digits.")
            .When(x => x.EmergencyAccessMode == 2);

        RuleFor(x => x.EmergencyPin)
            .NotEmpty()
            .WithMessage("Emergency PIN is required when EmergencyAccessMode is PinOnly (2).")
            .When(x => x.EmergencyAccessMode == 2);
    }
}

public sealed class CreateShareLinkRequestValidator : AbstractValidator<CreateShareLinkRequest>
{
    public CreateShareLinkRequestValidator()
    {
        RuleFor(x => x.ExpiryHours)
            .InclusiveBetween(1, 168).WithMessage("ExpiryHours must be between 1 and 168 (7 days).")
            .When(x => x.ExpiryHours.HasValue);
    }
}
