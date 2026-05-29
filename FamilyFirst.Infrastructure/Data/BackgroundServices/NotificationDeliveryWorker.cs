using FamilyFirst.Application.Services.Implementations;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class NotificationDeliveryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationDeliveryWorker> _logger;

    public NotificationDeliveryWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NotificationDeliveryWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification delivery worker failed during polling.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var pushNotificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        await notificationRepository.PurgeOlderThanAsync(DateTime.UtcNow.AddDays(-90), cancellationToken);
        var notifications = await notificationRepository.ListDueForImmediateDeliveryAsync(DateTime.UtcNow, cancellationToken);

        foreach (var notification in notifications)
        {
            var fcmToken = notification.RecipientUser?.FcmToken;

            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                notification.FcmMessageId = NotificationService.SuppressedFcmMessageId;
                await notificationRepository.UpdateAsync(notification, cancellationToken);
                continue;
            }

            var delivered = await pushNotificationService.SendPushAsync(
                fcmToken,
                notification.Title,
                notification.Body,
                cancellationToken,
                CreateDataPayload(notification));

            if (!delivered)
            {
                continue;
            }

            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;
            await notificationRepository.UpdateAsync(notification, cancellationToken);
        }
    }

    private static IReadOnlyDictionary<string, string>? CreateDataPayload(Domain.Entities.Notification notification)
    {
        var values = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(notification.DeepLinkPath))
        {
            values["deepLink"] = notification.DeepLinkPath;
        }

        if (notification.FamilyId.HasValue)
        {
            values["familyId"] = notification.FamilyId.Value.ToString();
        }

        if (notification.ReferenceId.HasValue)
        {
            values["referenceId"] = notification.ReferenceId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(notification.ReferenceType))
        {
            values["referenceType"] = notification.ReferenceType!;
        }

        return values.Count == 0 ? null : values;
    }
}
