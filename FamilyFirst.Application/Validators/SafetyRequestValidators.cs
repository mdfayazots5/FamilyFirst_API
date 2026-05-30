using FamilyFirst.Application.DTOs.Safety;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateSafeZoneRequestValidator : AbstractValidator<CreateSafeZoneRequest>
{
    public CreateSafeZoneRequestValidator()
    {
        RuleFor(x => x.ZoneName)
            .NotEmpty().WithMessage("ZoneName is required.")
            .MaximumLength(40).WithMessage("ZoneName must not exceed 40 characters.");

        RuleFor(x => x.ZoneType)
            .Must(SafeZoneType.All.Contains)
            .WithMessage($"ZoneType must be one of: {string.Join(", ", SafeZoneType.All)}.");

        RuleFor(x => x.RadiusMetres)
            .InclusiveBetween(50, 500)
            .WithMessage("RadiusMetres must be between 50 and 500.");

        RuleFor(x => x.AppliedToMemberIds)
            .NotEmpty().WithMessage("At least one member must be assigned to the zone.");

        RuleFor(x => x.LateAlertTime)
            .NotNull().WithMessage("LateAlertTime is required when LateAlertEnabled is true.")
            .When(x => x.LateAlertEnabled);
    }
}

public sealed class UpdateSafeZoneRequestValidator : AbstractValidator<UpdateSafeZoneRequest>
{
    public UpdateSafeZoneRequestValidator()
    {
        RuleFor(x => x.ZoneName)
            .NotEmpty()
            .MaximumLength(40).WithMessage("ZoneName must not exceed 40 characters.");

        RuleFor(x => x.ZoneType)
            .Must(SafeZoneType.All.Contains)
            .WithMessage($"ZoneType must be one of: {string.Join(", ", SafeZoneType.All)}.");

        RuleFor(x => x.RadiusMetres)
            .InclusiveBetween(50, 500)
            .WithMessage("RadiusMetres must be between 50 and 500.");

        RuleFor(x => x.LateAlertTime)
            .NotNull().WithMessage("LateAlertTime is required when LateAlertEnabled is true.")
            .When(x => x.LateAlertEnabled);
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.BatteryLevel).InclusiveBetween(0, 100);
    }
}

public sealed class TriggerSosRequestValidator : AbstractValidator<TriggerSosRequest>
{
    public TriggerSosRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
    }
}
