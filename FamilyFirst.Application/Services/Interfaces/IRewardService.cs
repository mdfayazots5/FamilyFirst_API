using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IRewardService
{
    Task<IReadOnlyCollection<RewardDto>> ListSystemRewardsAsync(
        Guid currentUserId,
        string? currentUserRole,
        CancellationToken cancellationToken);

    Task<RewardDto> CreateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        CreateRewardRequest request,
        CancellationToken cancellationToken);

    Task<RewardDto> UpdateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RewardDto>> ListFamilyRewardsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<RewardDto> CreateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        CreateRewardRequest request,
        CancellationToken cancellationToken);

    Task<RewardDto> UpdateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken);

    Task<RedemptionDto> RedeemAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid rewardId,
        RedeemRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RedemptionDto>> ListRedemptionsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid? childId,
        RedemptionStatus? status,
        CancellationToken cancellationToken);

    Task<RedemptionDto> ReviewRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid redemptionId,
        ReviewRedemptionRequest request,
        CancellationToken cancellationToken);
}

public interface IRewardRepository
{
    Task<Reward?> GetByIdAsync(Guid rewardId, CancellationToken cancellationToken);

    Task<Reward?> GetFamilyCopyByMasterRewardIdAsync(Guid familyId, Guid masterRewardId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Reward>> ListSystemRewardsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Reward>> ListFamilyRewardsAsync(Guid familyId, CancellationToken cancellationToken);

    Task AddAsync(Reward reward, CancellationToken cancellationToken);

    Task UpdateAsync(Reward reward, CancellationToken cancellationToken);
}

public interface IRewardRedemptionRepository
{
    Task<RewardRedemption?> GetByIdAsync(Guid redemptionId, CancellationToken cancellationToken);

    Task<RewardRedemption?> GetPendingByChildAndRewardAsync(Guid childProfileId, Guid rewardId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RewardRedemption>> ListByFamilyAsync(
        Guid familyId,
        Guid? childProfileId,
        RedemptionStatus? status,
        CancellationToken cancellationToken);

    Task AddAsync(RewardRedemption rewardRedemption, CancellationToken cancellationToken);

    Task UpdateAsync(RewardRedemption rewardRedemption, CancellationToken cancellationToken);

    Task ApplyApprovalAsync(
        RewardRedemption rewardRedemption,
        Reward reward,
        ChildProfile childProfile,
        CoinTransaction coinTransaction,
        CancellationToken cancellationToken);
}
