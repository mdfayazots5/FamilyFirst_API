using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class FamilyMemberRepository : IFamilyMemberRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public FamilyMemberRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FamilyMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken)
    {
        return QueryMembers()
            .SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);
    }

    public Task<FamilyMember?> GetActiveByFamilyAndUserAsync(Guid familyId, Guid userId, CancellationToken cancellationToken)
    {
        return QueryMembers()
            .SingleOrDefaultAsync(member =>
                member.FamilyId == familyId
                && member.UserId == userId
                && member.IsActive,
                cancellationToken);
    }

    public Task<FamilyMember?> GetPrimaryActiveMembershipForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return QueryMembers()
            .OrderByDescending(member => member.Role == UserRole.FamilyAdmin)
            .ThenBy(member => member.JoinedAt)
            .FirstOrDefaultAsync(member => member.UserId == userId && member.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FamilyMember>> ListActiveByFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await QueryMembers()
            .Where(member => member.FamilyId == familyId && member.IsActive)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountActiveByRoleAsync(Guid familyId, UserRole role, CancellationToken cancellationToken)
    {
        return _dbContext.FamilyMembers
            .CountAsync(member => member.FamilyId == familyId && member.Role == role && member.IsActive, cancellationToken);
    }

    public async Task AddAsync(FamilyMember familyMember, CancellationToken cancellationToken)
    {
        await _dbContext.FamilyMembers.AddAsync(familyMember, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FamilyMember familyMember, CancellationToken cancellationToken)
    {
        _dbContext.FamilyMembers.Update(familyMember);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<FamilyMember> QueryMembers()
    {
        return _dbContext.FamilyMembers
            .Include(member => member.User)
            .Include(member => member.Family)
            .ThenInclude(family => family!.Plan);
    }
}
