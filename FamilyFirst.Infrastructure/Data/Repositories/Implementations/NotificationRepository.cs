using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public NotificationRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        return QueryNotifications()
            .SingleOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> ListByRecipientAsync(
        Guid recipientUserId,
        bool? isRead,
        CancellationToken cancellationToken)
    {
        var query = QueryNotifications()
            .Where(notification => notification.RecipientUser!.Id == recipientUserId);

        if (isRead.HasValue)
        {
            query = query.Where(notification => notification.IsRead == isRead.Value);
        }

        return await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> ListDueForImmediateDeliveryAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        return await QueryNotifications()
            .Where(notification =>
                !notification.IsSent
                && notification.Priority != NotificationPriority.Urgent
                && !notification.IsBatched
                && (!notification.ScheduledFor.HasValue || notification.ScheduledFor <= asOfUtc))
            .OrderBy(notification => notification.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> ListDueBatchedAsync(
        string batchGroup,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        return await QueryNotifications()
            .Where(notification =>
                !notification.IsSent
                && notification.IsBatched
                && notification.BatchGroup == batchGroup
                && (!notification.ScheduledFor.HasValue || notification.ScheduledFor <= asOfUtc))
            .OrderBy(notification => notification.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Notification>().AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Notification>().AddRangeAsync(notifications, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        _dbContext.Set<Notification>().Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken)
    {
        _dbContext.Set<Notification>().UpdateRange(notifications);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken)
    {
        var notifications = await _dbContext.Set<Notification>()
            .Where(notification => notification.RecipientUser!.Id == recipientUserId && !notification.IsRead)
            .ToArrayAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = utcNow;
        }

        if (notifications.Length > 0)
        {
            _dbContext.Set<Notification>().UpdateRange(notifications);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return notifications.Length;
    }

    public async Task PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var notifications = await _dbContext.Set<Notification>()
            .Where(notification => notification.CreatedAt < cutoffUtc)
            .ToArrayAsync(cancellationToken);

        if (notifications.Length == 0)
        {
            return;
        }

        _dbContext.Set<Notification>().RemoveRange(notifications);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Notification> QueryNotifications()
    {
        return _dbContext.Set<Notification>()
            .Include(notification => notification.RecipientUser)
            .Include(notification => notification.Family);
    }
}
