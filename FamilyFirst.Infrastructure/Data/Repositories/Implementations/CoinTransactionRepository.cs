using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class CoinTransactionRepository : ICoinTransactionRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public CoinTransactionRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CoinTransaction>> ListByChildAsync(
        Guid familyId,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinTransactions
            .Where(transaction => transaction.FamilyId == familyId && transaction.ChildProfileId == childProfileId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task ApplyAsync(
        ChildProfile childProfile,
        CoinTransaction? coinTransaction,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.ChildProfiles.Update(childProfile);

        if (coinTransaction is not null)
        {
            await _dbContext.CoinTransactions.AddAsync(coinTransaction, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
