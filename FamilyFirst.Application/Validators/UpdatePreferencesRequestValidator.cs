using FamilyFirst.Application.DTOs.Notification;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(request => request.MorningDigestTime)
            .NotNull();

        RuleFor(request => request.EveningDigestTime)
            .NotNull();

        RuleFor(request => request.QuietHoursStartTime)
            .NotNull();

        RuleFor(request => request.QuietHoursEndTime)
            .NotNull();
    }
}
