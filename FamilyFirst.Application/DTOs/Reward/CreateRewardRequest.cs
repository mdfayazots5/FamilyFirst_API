namespace FamilyFirst.Application.DTOs.Reward;

public sealed class CreateRewardRequest
{
    public string RewardName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? IconCode { get; init; }

    public string Category { get; init; } = string.Empty;

    public int CoinCost { get; init; }
}
