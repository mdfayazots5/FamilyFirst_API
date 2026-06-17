using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class ChildProfileRepository : IChildProfileRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public ChildProfileRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChildProfile?> GetByIdAsync(Guid childProfileId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(profile => profile.Id == childProfileId, cancellationToken);
    }

    public Task<ChildProfile?> GetByFamilyMemberIdAsync(Guid familyMemberId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(profile => profile.FamilyMember!.Id == familyMemberId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ChildProfile>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await QueryProfiles()
            .Where(profile => profile.Family!.Id == familyId)
            .OrderBy(profile => profile.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ChildProfile>> ListByBirthdayAsync(
        int month,
        int day,
        CancellationToken cancellationToken)
    {
        return await QueryProfiles()
            .Where(profile =>
                profile.DateOfBirth.HasValue
                && profile.DateOfBirth.Value.Month == month
                && profile.DateOfBirth.Value.Day == day)
            .OrderBy(profile => profile.FamilyId)
            .ThenBy(profile => profile.DateOfBirth)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(ChildProfile childProfile, CancellationToken cancellationToken)
    {
        await _dbContext.ChildProfiles.AddAsync(childProfile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChildProfile childProfile, CancellationToken cancellationToken)
    {
        _dbContext.ChildProfiles.Update(childProfile);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ChildProfile> QueryProfiles()
    {
        return _dbContext.ChildProfiles
            .Include(profile => profile.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(profile => profile.User)
            .Include(profile => profile.Family);
    }
}
