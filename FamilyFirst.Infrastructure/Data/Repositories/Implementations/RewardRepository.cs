using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class RewardRepository : IRewardRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public RewardRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Reward?> GetByIdAsync(Guid rewardId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Reward>()
            .SingleOrDefaultAsync(reward => reward.Id == rewardId, cancellationToken);
    }

    public Task<Reward?> GetFamilyCopyByMasterRewardIdAsync(Guid familyId, Guid masterRewardId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Reward>()
            .SingleOrDefaultAsync(
                reward => reward.Family!.Id == familyId && reward.MasterReward!.Id == masterRewardId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Reward>> ListSystemRewardsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Reward>()
            .Where(reward => reward.IsSystem && !reward.FamilyId.HasValue)
            .OrderBy(reward => reward.Category)
            .ThenBy(reward => reward.CoinCost)
            .ThenBy(reward => reward.RewardName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Reward>> ListFamilyRewardsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Reward>()
            .Where(reward => reward.Family!.Id == familyId)
            .OrderBy(reward => reward.Category)
            .ThenBy(reward => reward.CoinCost)
            .ThenBy(reward => reward.RewardName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Reward reward, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Reward>().AddAsync(reward, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Reward reward, CancellationToken cancellationToken)
    {
        _dbContext.Set<Reward>().Update(reward);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
