using FamilyFirst.Application.DTOs.Attendance;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(request => request.SessionName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.SubjectName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.BatchName)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.BatchName));

        RuleFor(request => request.ScheduledDate)
            .NotNull()
            .Must(BeWithinAllowedScheduleWindow)
            .WithMessage("ScheduledDate must be within 7 days in the past and 30 days in the future.");

        RuleFor(request => request.StartTime).NotNull();

        RuleFor(request => request.EndTime)
            .Must((request, endTime) => !endTime.HasValue || !request.StartTime.HasValue || endTime.Value > request.StartTime.Value)
            .WithMessage("EndTime must be after StartTime.");

        RuleFor(request => request.RecurringDays)
            .NotNull()
            .Must(days => days is not null && days.Length > 0)
            .When(request => request.IsRecurring)
            .WithMessage("RecurringDays is required when IsRecurring is true.");

        RuleFor(request => request.RecurringDays)
            .Must(HaveValidRecurringDays)
            .WithMessage("RecurringDays must contain unique integer values from 1 through 7.");
    }

    private static bool BeWithinAllowedScheduleWindow(DateOnly? scheduledDate)
    {
        if (!scheduledDate.HasValue)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return scheduledDate.Value >= today.AddDays(-7)
            && scheduledDate.Value <= today.AddDays(30);
    }

    private static bool HaveValidRecurringDays(IReadOnlyCollection<int>? recurringDays)
    {
        if (recurringDays is null || recurringDays.Count == 0)
        {
            return true;
        }

        return recurringDays.All(day => day is >= 1 and <= 7)
            && recurringDays.Distinct().Count() == recurringDays.Count;
    }
}
