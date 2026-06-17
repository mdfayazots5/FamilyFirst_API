using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class RewardRedemptionRepository : IRewardRedemptionRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public RewardRedemptionRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RewardRedemption?> GetByIdAsync(Guid redemptionId, CancellationToken cancellationToken)
    {
        return QueryRedemptions()
            .SingleOrDefaultAsync(redemption => redemption.Id == redemptionId, cancellationToken);
    }

    public Task<RewardRedemption?> GetPendingByChildAndRewardAsync(
        Guid childProfileId,
        Guid rewardId,
        CancellationToken cancellationToken)
    {
        return QueryRedemptions()
            .SingleOrDefaultAsync(
                redemption => redemption.ChildProfile!.Id == childProfileId
                    && redemption.Reward!.Id == rewardId
                    && redemption.Status == RedemptionStatus.Pending,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<RewardRedemption>> ListByFamilyAsync(
        Guid familyId,
        Guid? childProfileId,
        RedemptionStatus? status,
        CancellationToken cancellationToken)
    {
        var query = QueryRedemptions()
            .Where(redemption => redemption.Family!.Id == familyId);

        if (childProfileId.HasValue)
        {
            query = query.Where(redemption => redemption.ChildProfile!.Id == childProfileId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(redemption => redemption.Status == status.Value);
        }

        return await query
            .OrderByDescending(redemption => redemption.RequestedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(RewardRedemption rewardRedemption, CancellationToken cancellationToken)
    {
        await _dbContext.Set<RewardRedemption>().AddAsync(rewardRedemption, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RewardRedemption rewardRedemption, CancellationToken cancellationToken)
    {
        _dbContext.Set<RewardRedemption>().Update(rewardRedemption);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyApprovalAsync(
        RewardRedemption rewardRedemption,
        Reward reward,
        ChildProfile childProfile,
        CoinTransaction coinTransaction,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Set<RewardRedemption>().Update(rewardRedemption);
        _dbContext.Set<Reward>().Update(reward);
        _dbContext.ChildProfiles.Update(childProfile);
        await _dbContext.Set<CoinTransaction>().AddAsync(coinTransaction, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<RewardRedemption> QueryRedemptions()
    {
        return _dbContext.Set<RewardRedemption>()
            .Include(redemption => redemption.Reward)
            .Include(redemption => redemption.ChildProfile)
            .ThenInclude(child => child!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(redemption => redemption.ChildProfile)
            .ThenInclude(child => child!.User)
            .Include(redemption => redemption.ReviewedByUser)
            .Include(redemption => redemption.Family);
    }
}
