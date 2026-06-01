using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Subscription : BaseEntity
{
    public long FamilyId { get; set; }

    public long PlanId { get; set; }

    public string Status { get; set; } = "Trial";

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? TrialEndDate { get; set; }

    public string? RazorpaySubscriptionId { get; set; }

    public string? RazorpayCustomerId { get; set; }

    public bool AutoRenew { get; set; } = true;

    public Family? Family { get; set; }

    public Plan? Plan { get; set; }
}
