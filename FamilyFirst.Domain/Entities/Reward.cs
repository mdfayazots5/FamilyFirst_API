using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Reward : BaseEntity
{
    public Guid? FamilyId { get; set; }

    public Guid? MasterRewardId { get; set; }

    public string RewardName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? IconCode { get; set; }

    public string Category { get; set; } = string.Empty;

    public int CoinCost { get; set; }

    public bool IsSystem { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int TimesRedeemedTotal { get; set; }

    public Family? Family { get; set; }

    public Reward? MasterReward { get; set; }
}
