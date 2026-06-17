using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class WeeklyDigestArchiveRepository : IWeeklyDigestArchiveRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public WeeklyDigestArchiveRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyCollection<WeeklyDigestArchive> Items, int TotalCount)> ListByFamilyAsync(
        Guid familyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WeeklyDigestArchives
            .Where(a => a.Family!.Id == familyId && !a.IsDeleted)
            .OrderByDescending(a => a.WeekStartDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }
}
