using System.Text.RegularExpressions;
using FamilyFirst.Application.DTOs.Calendar;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    private static readonly HashSet<string> AllowedVisibilityScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Family",
        "Parent",
        "Child",
        "Elder",
        "Caregiver"
    };

    private static readonly HashSet<int> AllowedReminderMinutes = new()
    {
        5,
        10,
        15,
        30,
        60,
        120,
        480,
        1440,
        4320
    };

    public UpdateEventRequestValidator()
    {
        RuleFor(request => request.EventTitle)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(300);

        RuleFor(request => request.EventType)
            .IsInEnum();

        RuleFor(request => request.Description)
            .MaximumLength(1000)
            .When(request => !string.IsNullOrWhiteSpace(request.Description));

        RuleFor(request => request.StartDateTime)
            .Must(startDateTime => startDateTime >= DateTime.UtcNow.AddYears(-1))
            .WithMessage("StartDateTime cannot be more than one year in the past.");

        RuleFor(request => request.EndDateTime)
            .Must((request, endDateTime) => !endDateTime.HasValue || endDateTime.Value >= request.StartDateTime)
            .WithMessage("EndDateTime must be greater than or equal to StartDateTime.");

        RuleFor(request => request.Location)
            .MaximumLength(300)
            .When(request => !string.IsNullOrWhiteSpace(request.Location));

        RuleFor(request => request.ColorHex)
            .Must(colorHex => string.IsNullOrWhiteSpace(colorHex) || Regex.IsMatch(colorHex, "^#[0-9A-Fa-f]{6}$"))
            .WithMessage("ColorHex must be a valid #RRGGBB value.");

        RuleFor(request => request.VisibilityScope)
            .Must(scope => AllowedVisibilityScopes.Contains(scope))
            .WithMessage($"VisibilityScope must be one of: {string.Join(", ", AllowedVisibilityScopes)}.");

        RuleFor(request => request.RecurrenceRule)
            .NotEmpty()
            .MaximumLength(200)
            .Must(LooksLikeRRule)
            .When(request => request.IsRecurring)
            .WithMessage("RecurrenceRule must be a valid RRULE-style string.");

        RuleFor(request => request.RecurrenceRule)
            .Must(string.IsNullOrWhiteSpace)
            .When(request => !request.IsRecurring)
            .WithMessage("RecurrenceRule must be empty when IsRecurring is false.");

        RuleFor(request => request.Reminders)
            .Must(reminders => reminders.Count <= 5)
            .WithMessage("A maximum of 5 reminders is allowed.")
            .Must(HaveUniqueReminderPairs)
            .WithMessage("Duplicate reminder entries are not allowed.");

        RuleForEach(request => request.Reminders)
            .ChildRules(reminder =>
            {
                reminder.RuleFor(item => item.RemindBeforeMinutes)
                    .Must(minutes => AllowedReminderMinutes.Contains(minutes))
                    .WithMessage("RemindBeforeMinutes must be one of the supported reminder values.");

                reminder.RuleFor(item => item.Channel)
                    .IsInEnum();
            });
    }

    private static bool HaveUniqueReminderPairs(IReadOnlyCollection<EventReminderRequest> reminders)
    {
        return reminders
            .Select(reminder => (reminder.RemindBeforeMinutes, reminder.Channel))
            .Distinct()
            .Count() == reminders.Count;
    }

    private static bool LooksLikeRRule(string? recurrenceRule)
    {
        if (string.IsNullOrWhiteSpace(recurrenceRule))
        {
            return false;
        }

        var parts = recurrenceRule.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0 || !parts[0].StartsWith("FREQ=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return parts.All(part =>
        {
            var separatorIndex = part.IndexOf('=');
            return separatorIndex > 0 && separatorIndex < part.Length - 1;
        });
    }
}
