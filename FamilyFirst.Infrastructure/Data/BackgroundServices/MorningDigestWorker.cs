using FamilyFirst.Application.Services.Implementations;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class MorningDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MorningDigestWorker> _logger;

    public MorningDigestWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<MorningDigestWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = ResolveNextRunUtc(now, 7);
            var delay = nextRunUtc - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                await ProcessDigestAsync(NotificationService.MorningDigestBatchGroup, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Morning digest worker failed.");
            }
        }
    }

    private async Task ProcessDigestAsync(string batchGroup, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var pushNotificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        await notificationRepository.PurgeOlderThanAsync(DateTime.UtcNow.AddDays(-90), cancellationToken);
        var notifications = await notificationRepository.ListDueBatchedAsync(batchGroup, DateTime.UtcNow, cancellationToken);

        foreach (var userGroup in notifications.GroupBy(notification => notification.RecipientUserId))
        {
            var userNotifications = userGroup.OrderBy(notification => notification.CreatedAt).ToArray();
            var firstNotification = userNotifications[0];
            var fcmToken = firstNotification.RecipientUser?.FcmToken;

            if (!string.IsNullOrWhiteSpace(fcmToken))
            {
                var delivered = await pushNotificationService.SendPushAsync(
                    fcmToken,
                    "Morning digest",
                    BuildDigestBody(userNotifications),
                    cancellationToken,
                    new Dictionary<string, string>
                    {
                        ["deepLink"] = "/home"
                    });

                if (!delivered)
                {
                    continue;
                }
            }

            foreach (var notification in userNotifications)
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                notification.FcmMessageId ??= NotificationService.SuppressedFcmMessageId;
            }

            await notificationRepository.UpdateRangeAsync(userNotifications, cancellationToken);
        }
    }

    private static DateTime ResolveNextRunUtc(DateTime utcNow, int hour)
    {
        var nextRun = utcNow.Date.AddHours(hour);

        return utcNow < nextRun ? nextRun : nextRun.AddDays(1);
    }

    private static string BuildDigestBody(IReadOnlyCollection<Domain.Entities.Notification> notifications)
    {
        if (notifications.Count == 1)
        {
            return notifications.First().Body;
        }

        var preview = string.Join("; ", notifications.Take(3).Select(notification => notification.Title));

        return $"You have {notifications.Count} updates this morning: {preview}";
    }
}
