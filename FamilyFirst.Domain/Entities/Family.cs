using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Family : BaseEntity
{
    public string FamilyName { get; set; } = string.Empty;

    public string JoinCode { get; set; } = string.Empty;

    public string? City { get; set; }

    public long PlanId { get; set; }

    public long? SubscriptionId { get; set; }

    public long FamilyAdminUserId { get; set; }

    public int FamilyScore { get; set; }

    public DateTime? FamilyScoreUpdatedAt { get; set; }

    public int CurrentStreakDays { get; set; }

    public int BestStreakDays { get; set; }

    public string TimezoneId { get; set; } = "Asia/Kolkata";

    public bool IsActive { get; set; } = true;

    public Plan? Plan { get; set; }

    public Subscription? Subscription { get; set; }

    public User? FamilyAdminUser { get; set; }

    public ICollection<FamilyMember> Members { get; } = new List<FamilyMember>();
}
