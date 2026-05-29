using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Reward;

public sealed record RedemptionDto(
    Guid RedemptionId,
    Guid RewardId,
    Guid ChildProfileId,
    Guid FamilyId,
    int CoinsSpent,
    RedemptionStatus Status,
    DateTime RequestedAt,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ParentNote,
    string RewardName,
    string ChildName);

public sealed class RedeemRequest
{
    public Guid ChildProfileId { get; init; }
}
