using FamilyFirst.Application.DTOs.Medical;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateHealthProfileRequestValidator : AbstractValidator<UpdateHealthProfileRequest>
{
    private static readonly HashSet<string> ValidBloodGroups = new()
    {
        "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", ""
    };

    private static readonly HashSet<string> ValidAllergyCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Food", "Medication", "Environmental"
    };

    public UpdateHealthProfileRequestValidator()
    {
        RuleFor(x => x.BloodGroup)
            .Must(bg => ValidBloodGroups.Contains(bg))
            .WithMessage("BloodGroup must be one of: A+, A-, B+, B-, AB+, AB-, O+, O- (or empty string).");

        RuleForEach(x => x.KnownAllergies)
            .Must(a => !string.IsNullOrWhiteSpace(a.Text))
            .WithMessage("Each allergy must have a non-empty text value.")
            .When(x => x.KnownAllergies != null);

        RuleForEach(x => x.KnownAllergies)
            .Must(a => ValidAllergyCategories.Contains(a.Category))
            .WithMessage("Allergy category must be Food, Medication, or Environmental.")
            .When(x => x.KnownAllergies != null);
    }
}

public sealed class AddPrescriptionRequestValidator : AbstractValidator<AddPrescriptionRequest>
{
    public AddPrescriptionRequestValidator()
    {
        RuleFor(x => x.MedicationName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrescribingDoctor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .Must((req, end) => end == null || end >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.")
            .When(x => x.EndDate.HasValue);
    }
}

public sealed class AddVaccinationRequestValidator : AbstractValidator<AddVaccinationRequest>
{
    public AddVaccinationRequestValidator()
    {
        RuleFor(x => x.VaccineName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status)
            .Must(s => s is VaccinationStatus.Given or VaccinationStatus.Due
                         or VaccinationStatus.Overdue or VaccinationStatus.NotApplicable)
            .WithMessage("Status must be: Given, Due, Overdue, or NotApplicable.");
        RuleFor(x => x.GivenDate)
            .NotEmpty().WithMessage("GivenDate is required when Status is Given.")
            .When(x => x.Status == VaccinationStatus.Given);
    }
}

public sealed class AddHealthRecordRequestValidator : AbstractValidator<AddHealthRecordRequest>
{
    public AddHealthRecordRequestValidator()
    {
        RuleFor(x => x.EventType)
            .Must(HealthRecordEventType.All.Contains)
            .WithMessage("EventType must be one of: Prescription, Vaccination, HospitalVisit, TestReport, DoctorNote, AllergyUpdate.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}

public sealed class ShareEmergencyCardRequestValidator : AbstractValidator<ShareEmergencyCardRequest>
{
    public ShareEmergencyCardRequestValidator()
    {
        RuleFor(x => x.ExpiryHours)
            .InclusiveBetween(1, 168)
            .WithMessage("ExpiryHours must be between 1 and 168.")
            .When(x => x.ExpiryHours.HasValue);
    }
}

public sealed class AddHeightWeightRequestValidator : AbstractValidator<AddHeightWeightRequest>
{
    public AddHeightWeightRequestValidator()
    {
        RuleFor(x => x)
            .Must(r => r.HeightCm.HasValue || r.WeightKg.HasValue)
            .WithMessage("At least one of HeightCm or WeightKg must be provided.");
        RuleFor(x => x.HeightCm)
            .InclusiveBetween(20m, 250m).When(x => x.HeightCm.HasValue)
            .WithMessage("HeightCm must be between 20 and 250.");
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(1m, 300m).When(x => x.WeightKg.HasValue)
            .WithMessage("WeightKg must be between 1 and 300.");
    }
}
