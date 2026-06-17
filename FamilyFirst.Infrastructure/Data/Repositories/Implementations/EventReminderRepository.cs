using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class EventReminderRepository : IEventReminderRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public EventReminderRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddEventGraphAsync(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminder> reminders,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Set<CalendarEvent>().AddAsync(calendarEvent, cancellationToken);

        if (reminders.Count > 0)
        {
            await _dbContext.Set<EventReminder>().AddRangeAsync(reminders, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateEventGraphAsync(
        CalendarEvent calendarEvent,
        IReadOnlyCollection<EventReminder> reminders,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Set<CalendarEvent>().Update(calendarEvent);

        var existingReminders = await _dbContext.Set<EventReminder>()
            .Where(reminder => reminder.CalendarEventId == calendarEvent.InternalId && !reminder.IsDeleted)
            .ToArrayAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;

        foreach (var existingReminder in existingReminders)
        {
            existingReminder.IsDeleted = true;
            existingReminder.DeletedAt = utcNow;
            _dbContext.Set<EventReminder>().Update(existingReminder);
        }

        if (reminders.Count > 0)
        {
            await _dbContext.Set<EventReminder>().AddRangeAsync(reminders, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SoftDeleteEventGraphAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        calendarEvent.IsDeleted = true;
        calendarEvent.DeletedAt = utcNow;
        _dbContext.Set<CalendarEvent>().Update(calendarEvent);

        var reminders = await _dbContext.Set<EventReminder>()
            .Where(reminder => reminder.CalendarEventId == calendarEvent.InternalId && !reminder.IsDeleted)
            .ToArrayAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            reminder.IsDeleted = true;
            reminder.DeletedAt = utcNow;
            _dbContext.Set<EventReminder>().Update(reminder);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventReminder>> ListDuePendingAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        return await QueryReminders()
            .Where(reminder => !reminder.IsSent && reminder.ScheduledFor <= asOfUtc)
            .OrderBy(reminder => reminder.ScheduledFor)
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateAsync(EventReminder reminder, CancellationToken cancellationToken)
    {
        _dbContext.Set<EventReminder>().Update(reminder);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<EventReminder> QueryReminders()
    {
        return _dbContext.Set<EventReminder>()
            .Include(reminder => reminder.Event)
            .ThenInclude(calendarEvent => calendarEvent!.CreatedByUser)
            .Include(reminder => reminder.Event)
            .ThenInclude(calendarEvent => calendarEvent!.LinkedChildProfile)
            .ThenInclude(childProfile => childProfile!.User)
            .Include(reminder => reminder.Family);
    }
}
