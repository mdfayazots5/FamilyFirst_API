namespace FamilyFirst.Application.DTOs.Reward;

public sealed class UpdateRewardRequest
{
    public string RewardName { get; init; } = string.Empty;

    public int CoinCost { get; init; }

    public bool IsEnabled { get; init; }
}
