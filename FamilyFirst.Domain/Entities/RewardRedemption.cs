using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class RewardRedemption : BaseEntity
{
    public long RewardId { get; set; }

    public long ChildProfileId { get; set; }

    public long FamilyId { get; set; }

    public int CoinsSpent { get; set; }

    public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public long? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ParentNote { get; set; }

    public Reward? Reward { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? ReviewedByUser { get; set; }
}
