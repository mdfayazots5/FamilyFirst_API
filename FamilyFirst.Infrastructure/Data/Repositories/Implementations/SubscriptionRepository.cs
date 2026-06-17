using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public SubscriptionRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Subscription?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Subscriptions
            .Include(subscription => subscription.Plan)
            .SingleOrDefaultAsync(subscription => subscription.Family!.Id == familyId, cancellationToken);
    }
}
