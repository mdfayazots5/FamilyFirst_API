using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class CalendarEventRepository : ICalendarEventRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public CalendarEventRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CalendarEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return QueryEvents()
            .SingleOrDefaultAsync(calendarEvent => calendarEvent.Id == eventId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CalendarEvent>> ListByFamilyAsync(
        Guid familyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = QueryEvents()
            .Where(calendarEvent => calendarEvent.FamilyId == familyId);

        if (fromDate.HasValue)
        {
            query = query.Where(calendarEvent =>
                (calendarEvent.EndDateTime ?? calendarEvent.StartDateTime) >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(calendarEvent => calendarEvent.StartDateTime <= toDate.Value);
        }

        return await query
            .OrderBy(calendarEvent => calendarEvent.StartDateTime)
            .ThenBy(calendarEvent => calendarEvent.EventTitle)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CalendarEvent?> GetBirthdayEventAsync(
        Guid familyId,
        Guid linkedChildProfileId,
        DateOnly eventDate,
        CancellationToken cancellationToken)
    {
        var dayStart = eventDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var nextDayStart = dayStart.AddDays(1);

        return QueryEvents()
            .SingleOrDefaultAsync(
                calendarEvent => calendarEvent.FamilyId == familyId
                    && calendarEvent.LinkedChildProfileId == linkedChildProfileId
                    && calendarEvent.EventType == Domain.Enums.EventType.Birthday
                    && calendarEvent.StartDateTime >= dayStart
                    && calendarEvent.StartDateTime < nextDayStart,
                cancellationToken);
    }

    private IQueryable<CalendarEvent> QueryEvents()
    {
        return _dbContext.Set<CalendarEvent>()
            .Include(calendarEvent => calendarEvent.Family)
            .Include(calendarEvent => calendarEvent.CreatedByUser)
            .Include(calendarEvent => calendarEvent.LinkedChildProfile)
            .ThenInclude(childProfile => childProfile!.User)
            .Include(calendarEvent => calendarEvent.LinkedChildProfile)
            .ThenInclude(childProfile => childProfile!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(calendarEvent => calendarEvent.Reminders);
    }
}
