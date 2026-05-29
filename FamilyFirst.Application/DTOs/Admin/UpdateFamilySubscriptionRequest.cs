namespace FamilyFirst.Application.DTOs.Admin;

public sealed class UpdateFamilySubscriptionRequest
{
    public int PlanId { get; init; }

    public int? ExtendTrialDays { get; init; }

    public string? Status { get; init; }
}
