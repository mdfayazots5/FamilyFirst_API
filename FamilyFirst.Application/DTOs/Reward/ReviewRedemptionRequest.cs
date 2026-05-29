using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Reward;

public sealed class ReviewRedemptionRequest
{
    public RedemptionStatus Status { get; init; }

    public string? ParentNote { get; init; }
}
