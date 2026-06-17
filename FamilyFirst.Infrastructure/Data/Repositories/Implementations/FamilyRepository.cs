using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class FamilyRepository : IFamilyRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public FamilyRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Plan?> GetPlanByCodeAsync(string planCode, CancellationToken cancellationToken)
    {
        return _dbContext.Plans
            .SingleOrDefaultAsync(plan => plan.PlanCode == planCode && plan.IsActive, cancellationToken);
    }

    public Task<Plan?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        return _dbContext.Plans
            .SingleOrDefaultAsync(plan => plan.PlanId == planId && plan.IsActive, cancellationToken);
    }

    public Task<Family?> GetByIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Families
            .Include(family => family.Plan)
            .SingleOrDefaultAsync(family => family.Id == familyId && family.IsActive, cancellationToken);
    }

    public Task<Family?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken)
    {
        return _dbContext.Families
            .Include(family => family.Plan)
            .SingleOrDefaultAsync(family => family.JoinCode == joinCode && family.IsActive, cancellationToken);
    }

    public Task<bool> ExistsByJoinCodeAsync(string joinCode, CancellationToken cancellationToken)
    {
        return _dbContext.Families
            .AnyAsync(family => family.JoinCode == joinCode, cancellationToken);
    }

    public Task<bool> UserOwnsActiveFamilyAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Families
            .AnyAsync(family => family.FamilyAdminUser!.Id == userId && family.IsActive, cancellationToken);
    }

    public async Task AddFamilyGraphAsync(Family family, Subscription subscription, FamilyMember familyMember, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        family.SubscriptionId = null;
        await _dbContext.Families.AddAsync(family, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        subscription.FamilyId = family.InternalId;
        await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        family.SubscriptionId = subscription.InternalId;
        await _dbContext.FamilyMembers.AddAsync(familyMember, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Family family, CancellationToken cancellationToken)
    {
        _dbContext.Families.Update(family);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
