using FamilyFirst.Application.DTOs.Task;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateTaskTemplateRequestValidator : AbstractValidator<CreateTaskTemplateRequest>
{
    public CreateTaskTemplateRequestValidator()
    {
        RuleFor(request => request.TaskName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => request.Instructions)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.Instructions));

        RuleFor(request => request.IconCode)
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(request.IconCode));

        RuleFor(request => request.TimeBlock)
            .Must(TaskMetadata.IsAllowedTaskTimeBlock)
            .WithMessage("TimeBlock must be valid and cannot be School.");

        RuleFor(request => request.DurationMinutes)
            .InclusiveBetween(5, 120);

        RuleFor(request => request.CoinValue)
            .InclusiveBetween(5, 200);

        RuleFor(request => request.PillarTag)
            .Must(TaskMetadata.IsValidPillarTag)
            .WithMessage($"PillarTag must be one of: {string.Join(", ", TaskMetadata.AllowedPillarTags)}.")
            .When(request => !string.IsNullOrWhiteSpace(request.PillarTag));

        RuleFor(request => request.RecurringDays)
            .Must((request, recurringDays) => TaskMetadata.IsValidRecurringDays(recurringDays, request.IsRecurring))
            .WithMessage("RecurringDays must be unique values from 1 to 7, and at least one day is required when IsRecurring is true.");

        RuleFor(request => request.ActiveFromDate)
            .Must(BeWithinAllowedActiveFromRange)
            .WithMessage("ActiveFromDate cannot be more than 30 days in the past or more than 1 year in the future.");

        RuleFor(request => request.Category)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.AgeGroup)
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(request.AgeGroup));
    }

    private static bool BeWithinAllowedActiveFromRange(DateOnly activeFromDate)
    {
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);

        return activeFromDate >= utcToday.AddDays(-30) && activeFromDate <= utcToday.AddYears(1);
    }
}
