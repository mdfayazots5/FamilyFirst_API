using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public NotificationPreferenceRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<NotificationPreference>()
            .Include(preference => preference.User)
            .Include(preference => preference.Family)
            .SingleOrDefaultAsync(preference => preference.User!.Id == userId, cancellationToken);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken)
    {
        await _dbContext.Set<NotificationPreference>().AddAsync(preference, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken)
    {
        _dbContext.Set<NotificationPreference>().Update(preference);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
