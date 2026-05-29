using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class AdminFamilySearchRequestValidator : AbstractValidator<AdminFamilySearchRequest>
{
    public AdminFamilySearchRequestValidator()
    {
        RuleFor(request => request.Query)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.Query));

        RuleFor(request => request.PlanCode)
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(request.PlanCode));

        RuleFor(request => request.Page)
            .GreaterThan(0);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);
    }
}

public sealed class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(request => request.PlanName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.PriceMonthly)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.MaxChildren)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.MaxTeachers)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.StorageQuotaMb)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.TrialDays)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateFeatureFlagRequestValidator : AbstractValidator<UpdateFeatureFlagRequest>
{
    public UpdateFeatureFlagRequestValidator()
    {
        RuleFor(request => request.FlagValue)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(300)
            .When(request => !string.IsNullOrWhiteSpace(request.Description));
    }
}

public sealed class NotificationCampaignRequestValidator : AbstractValidator<NotificationCampaignRequest>
{
    public NotificationCampaignRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Body)
            .NotEmpty()
            .MaximumLength(1000);

        RuleForEach(request => request.Roles)
            .Must(role => Enum.TryParse<UserRole>(role, true, out _))
            .WithMessage("Roles contain an unsupported user role.");

        RuleForEach(request => request.PlanCodes)
            .MaximumLength(50);

        RuleFor(request => request.DeepLinkPath)
            .MaximumLength(300)
            .When(request => !string.IsNullOrWhiteSpace(request.DeepLinkPath));
    }
}
