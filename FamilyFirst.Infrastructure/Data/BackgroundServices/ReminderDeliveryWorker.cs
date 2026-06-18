using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class ReminderDeliveryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryBackoff = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ReminderDeliveryWorker> _logger;

    public ReminderDeliveryWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ReminderDeliveryWorker> logger)
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
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Reminder delivery worker failed during polling.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var reminderRepository = scope.ServiceProvider.GetRequiredService<IEventReminderRepository>();
        var preferenceService = scope.ServiceProvider.GetRequiredService<INotificationPreferenceService>();
        var pushNotificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var dueReminders = await reminderRepository.ListDuePendingAsync(DateTime.UtcNow, cancellationToken);

        foreach (var reminder in dueReminders)
        {
            var calendarEvent = reminder.Event;
            var createdByUser = calendarEvent?.CreatedByUser;

            if (calendarEvent is null || createdByUser is null)
            {
                _logger.LogWarning("Reminder {ReminderId} skipped because the event creator context is missing.", reminder.Id);
                continue;
            }

            var preference = await preferenceService.GetOrCreatePreferencesAsync(
                createdByUser.Id,
                cancellationToken);

            if (!preference.CalendarAlerts)
            {
                reminder.IsSent = true;
                reminder.SentAt = DateTime.UtcNow;
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                continue;
            }

            var isUrgentReminder = IsUrgentReminder(reminder);

            if (preference.QuietHoursEnabled
                && !isUrgentReminder
                && IsWithinQuietHours(DateTime.UtcNow, preference))
            {
                reminder.ScheduledFor = ResolveNextMorningDigestUtc(DateTime.UtcNow, preference);
                await reminderRepository.UpdateAsync(reminder, cancellationToken);
                continue;
            }

            var fcmToken = createdByUser.FcmToken;

            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                _logger.LogInformation(
                    "Reminder {ReminderId} cannot be delivered because the creator has no FCM token.",
                    reminder.Id);
                continue;
            }

            var familyId = reminder.Family?.Id ?? calendarEvent.Family?.Id ?? Guid.Empty;
            var eventId = calendarEvent.Id;
            var deepLink = $"/families/{familyId}/calendar/events/{eventId}";
            var title = isUrgentReminder ? "Urgent reminder" : "Event reminder";
            var body = $"{calendarEvent.EventTitle} at {calendarEvent.StartDateTime:yyyy-MM-dd HH:mm} UTC";
            var delivered = false;

            for (var attempt = 1; attempt <= 3 && !delivered; attempt++)
            {
                delivered = await pushNotificationService.SendPushAsync(
                    fcmToken,
                    title,
                    body,
                    cancellationToken,
                    new Dictionary<string, string>
                    {
                        ["deepLink"] = deepLink,
                        ["familyId"] = familyId.ToString(),
                        ["eventId"] = eventId.ToString()
                    });

                if (delivered)
                {
                    break;
                }

                _logger.LogWarning(
                    "Reminder delivery attempt {Attempt} failed for reminder {ReminderId}.",
                    attempt,
                    reminder.Id);

                if (attempt < 3)
                {
                    await Task.Delay(RetryBackoff, cancellationToken);
                }
            }

            if (!delivered)
            {
                _logger.LogError(
                    "Reminder {ReminderId} failed after 3 delivery attempts.",
                    reminder.Id);
                continue;
            }

            reminder.IsSent = true;
            reminder.SentAt = DateTime.UtcNow;
            await reminderRepository.UpdateAsync(reminder, cancellationToken);
        }
    }

    private static bool IsUrgentReminder(EventReminder reminder)
    {
        return reminder.Event?.EventType == EventType.MedicineReminder;
    }

    private static bool IsWithinQuietHours(DateTime utcNow, NotificationPreference preference)
    {
        var currentTime = TimeOnly.FromDateTime(utcNow);
        var start = TimeOnly.FromDateTime(preference.QuietHoursStartTime);
        var end = TimeOnly.FromDateTime(preference.QuietHoursEndTime);

        if (start == end)
        {
            return true;
        }

        if (start < end)
        {
            return currentTime >= start && currentTime < end;
        }

        return currentTime >= start || currentTime < end;
    }

    private static DateTime ResolveNextMorningDigestUtc(DateTime utcNow, NotificationPreference preference)
    {
        var currentDate = DateOnly.FromDateTime(utcNow);
        var currentTime = TimeOnly.FromDateTime(utcNow);
        var morningDigestTime = TimeOnly.FromDateTime(preference.MorningDigestTime);
        var digestDate = currentTime <= morningDigestTime
            ? currentDate
            : currentDate.AddDays(1);

        return digestDate.ToDateTime(morningDigestTime, DateTimeKind.Utc);
    }
}
