using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ICoinService
{
    Task<CoinTransactionDto> EarnCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childProfileId,
        int amount,
        string referenceType,
        Guid? referenceId,
        string? note,
        string? pillarTag,
        CancellationToken cancellationToken);

    Task<CoinTransactionDto> DeductCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childProfileId,
        DeductCoinsRequest request,
        CancellationToken cancellationToken);

    Task<CoinTransactionDto> SpendCoinsForRewardRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Reward reward,
        RewardRedemption rewardRedemption,
        CancellationToken cancellationToken);

    Task<PaginatedList<CoinTransactionDto>> GetHistoryAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childProfileId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> UseStreakFreezeAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childProfileId,
        CancellationToken cancellationToken);
}

public interface ICoinTransactionRepository
{
    Task<IReadOnlyCollection<CoinTransaction>> ListByChildAsync(Guid familyId, Guid childProfileId, CancellationToken cancellationToken);

    Task ApplyAsync(ChildProfile childProfile, CoinTransaction? coinTransaction, CancellationToken cancellationToken);
}
