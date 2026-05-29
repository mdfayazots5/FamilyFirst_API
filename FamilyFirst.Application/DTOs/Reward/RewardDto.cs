namespace FamilyFirst.Application.DTOs.Reward;

public sealed record RewardDto(
    Guid RewardId,
    Guid? FamilyId,
    Guid? MasterRewardId,
    string RewardName,
    string? Description,
    string? IconCode,
    string Category,
    int CoinCost,
    bool IsSystem,
    bool IsEnabled,
    int TimesRedeemedTotal);
