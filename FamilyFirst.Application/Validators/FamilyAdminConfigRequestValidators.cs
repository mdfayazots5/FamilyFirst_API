using FamilyFirst.Application.DTOs.Admin;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateNotificationRuleRequestValidator : AbstractValidator<UpdateNotificationRuleRequest>
{
    public UpdateNotificationRuleRequestValidator()
    {
        RuleFor(request => request.DeliveryDelayMinutes)
            .InclusiveBetween(0, 1440)
            .When(request => request.DeliveryDelayMinutes.HasValue);
    }
}

public sealed class CreateCustomAttendanceStatusRequestValidator : AbstractValidator<CreateCustomAttendanceStatusRequest>
{
    public CreateCustomAttendanceStatusRequestValidator()
    {
        RuleFor(request => request.StatusName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.ColorHex)
            .NotEmpty()
            .Matches("^#[0-9A-Fa-f]{6}$");
    }
}
