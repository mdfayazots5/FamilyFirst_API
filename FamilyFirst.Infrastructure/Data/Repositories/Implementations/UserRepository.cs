using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class UserRepository : IUserRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public UserRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdForFamilyAsync(Guid id, long familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == id
                && _dbContext.FamilyMembers.Any(member => member.UserId == user.InternalId
                    && member.FamilyId == familyId && member.IsActive), cancellationToken);
    }

    public Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .SingleOrDefaultAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);
    }

    public Task RegisterFailedLoginAsync(
        long userInternalId,
        int maxAttempts,
        DateTime lockoutUntilUtc,
        CancellationToken cancellationToken)
    {
        // Both SET expressions read the pre-update row values, so the increment and the lockout decision are atomic.
        return _dbContext.Users
            .Where(user => user.InternalId == userInternalId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.LockoutUntil,
                    user => user.FailedLoginAttemptCount + 1 >= maxAttempts ? lockoutUntilUtc : user.LockoutUntil)
                .SetProperty(user => user.FailedLoginAttemptCount,
                    user => user.FailedLoginAttemptCount + 1 >= maxAttempts ? 0 : user.FailedLoginAttemptCount + 1),
                cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
