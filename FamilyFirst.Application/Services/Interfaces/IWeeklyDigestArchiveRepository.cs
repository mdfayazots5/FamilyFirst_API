using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IWeeklyDigestArchiveRepository
{
    Task<(IReadOnlyCollection<WeeklyDigestArchive> Items, int TotalCount)> ListByFamilyAsync(
        Guid familyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
