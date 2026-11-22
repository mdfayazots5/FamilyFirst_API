using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.Data.SqlClient;
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
        return QueryPreferences()
            .SingleOrDefaultAsync(preference => preference.User!.Id == userId, cancellationToken);
    }

    public async Task<NotificationPreference> AddOrGetAsync(NotificationPreference preference, CancellationToken cancellationToken)
    {
        await _dbContext.Set<NotificationPreference>().AddAsync(preference, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return preference;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _dbContext.Entry(preference).State = EntityState.Detached;
            var existing = await QueryPreferences()
                .SingleOrDefaultAsync(item => item.UserId == preference.UserId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            throw;
        }
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken)
    {
        _dbContext.Set<NotificationPreference>().Update(preference);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<NotificationPreference> QueryPreferences()
    {
        return _dbContext.Set<NotificationPreference>()
            .Include(preference => preference.User)
            .Include(preference => preference.Family);
    }
}
