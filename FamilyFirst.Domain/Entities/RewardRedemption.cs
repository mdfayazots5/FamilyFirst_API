using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class RewardRedemption : BaseEntity
{
    public Guid RewardId { get; set; }

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public int CoinsSpent { get; set; }

    public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ParentNote { get; set; }

    public Reward? Reward { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? ReviewedByUser { get; set; }
}
