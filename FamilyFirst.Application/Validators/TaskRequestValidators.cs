using FamilyFirst.Application.DTOs.Task;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        Include(new TaskRequestRules<CreateTaskRequest>(
            request => request.TaskName,
            request => request.Instructions,
            request => request.IconCode,
            request => request.TimeBlock,
            request => request.DurationMinutes,
            request => request.CoinValue,
            request => request.PillarTag,
            request => request.IsRecurring,
            request => request.RecurringDays,
            request => request.ActiveFromDate,
            request => request.ChildProfileId));
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        Include(new TaskRequestRules<UpdateTaskRequest>(
            request => request.TaskName,
            request => request.Instructions,
            request => request.IconCode,
            request => request.TimeBlock,
            request => request.DurationMinutes,
            request => request.CoinValue,
            request => request.PillarTag,
            request => request.IsRecurring,
            request => request.RecurringDays,
            request => request.ActiveFromDate,
            request => request.ChildProfileId));
    }
}

internal sealed class TaskRequestRules<T> : AbstractValidator<T>
{
    public TaskRequestRules(
        Func<T, string> taskNameAccessor,
        Func<T, string?> instructionsAccessor,
        Func<T, string?> iconCodeAccessor,
        Func<T, FamilyFirst.Domain.Enums.TaskTimeBlock> timeBlockAccessor,
        Func<T, int> durationMinutesAccessor,
        Func<T, int> coinValueAccessor,
        Func<T, string?> pillarTagAccessor,
        Func<T, bool> isRecurringAccessor,
        Func<T, IReadOnlyCollection<int>?> recurringDaysAccessor,
        Func<T, DateOnly> activeFromDateAccessor,
        Func<T, Guid?> childProfileIdAccessor)
    {
        RuleFor(request => taskNameAccessor(request))
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(request => instructionsAccessor(request))
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(instructionsAccessor(request)));

        RuleFor(request => iconCodeAccessor(request))
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(iconCodeAccessor(request)));

        RuleFor(request => timeBlockAccessor(request))
            .Must(TaskMetadata.IsAllowedTaskTimeBlock)
            .WithMessage("TimeBlock must be valid and cannot be School.");

        RuleFor(request => durationMinutesAccessor(request))
            .InclusiveBetween(5, 120);

        RuleFor(request => coinValueAccessor(request))
            .InclusiveBetween(5, 200);

        RuleFor(request => pillarTagAccessor(request))
            .Must(TaskMetadata.IsValidPillarTag)
            .WithMessage($"PillarTag must be one of: {string.Join(", ", TaskMetadata.AllowedPillarTags)}.")
            .When(request => !string.IsNullOrWhiteSpace(pillarTagAccessor(request)));

        RuleFor(request => recurringDaysAccessor(request))
            .Must((request, recurringDays) => TaskMetadata.IsValidRecurringDays(recurringDays, isRecurringAccessor(request)))
            .WithMessage("RecurringDays must be unique values from 1 to 7, and at least one day is required when IsRecurring is true.");

        RuleFor(request => activeFromDateAccessor(request))
            .Must(BeWithinAllowedActiveFromRange)
            .WithMessage("ActiveFromDate cannot be more than 30 days in the past or more than 1 year in the future.");

        RuleFor(request => childProfileIdAccessor(request))
            .Must(childProfileId => !childProfileId.HasValue || childProfileId.Value != Guid.Empty)
            .WithMessage("ChildProfileId must be a valid value when provided.");
    }

    private static bool BeWithinAllowedActiveFromRange(DateOnly activeFromDate)
    {
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);

        return activeFromDate >= utcToday.AddDays(-30) && activeFromDate <= utcToday.AddYears(1);
    }
}
