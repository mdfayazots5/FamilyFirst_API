using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Subscription : BaseEntity
{
    public Guid FamilyId { get; set; }

    public int PlanId { get; set; }

    public string Status { get; set; } = "Trial";

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly? TrialEndDate { get; set; }

    public string? RazorpaySubscriptionId { get; set; }

    public string? RazorpayCustomerId { get; set; }

    public bool AutoRenew { get; set; } = true;

    public Family? Family { get; set; }

    public Plan? Plan { get; set; }
}
