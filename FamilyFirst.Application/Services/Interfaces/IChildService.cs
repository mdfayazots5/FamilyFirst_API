using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Domain.Entities;
using TaskDeductCoinsRequest = FamilyFirst.Application.DTOs.Task.DeductCoinsRequest;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IChildService
{
    Task<IReadOnlyCollection<ChildSummaryDto>> ListChildrenAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<ChildDetailDto> GetChildAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid childId, CancellationToken cancellationToken);

    Task<ChildDetailDto> UpdateChildAsync(Guid currentUserId, Guid familyId, Guid childId, UpdateChildRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ScoreHistoryDto>> GetScoreHistoryAsync(Guid currentUserId, Guid familyId, Guid childId, CancellationToken cancellationToken);

    Task<PaginatedList<CoinTransactionDto>> GetCoinHistoryAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CoinTransactionDto> DeductCoinsAsync(Guid currentUserId, Guid familyId, Guid childId, TaskDeductCoinsRequest request, CancellationToken cancellationToken);

    Task<bool> UseStreakFreezeAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken);
}

public interface IChildProfileRepository
{
    Task<ChildProfile?> GetByIdAsync(Guid childProfileId, CancellationToken cancellationToken);

    Task<ChildProfile?> GetByFamilyMemberIdAsync(Guid familyMemberId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ChildProfile>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ChildProfile>> ListByBirthdayAsync(int month, int day, CancellationToken cancellationToken);

    Task AddAsync(ChildProfile childProfile, CancellationToken cancellationToken);

    Task UpdateAsync(ChildProfile childProfile, CancellationToken cancellationToken);
}
