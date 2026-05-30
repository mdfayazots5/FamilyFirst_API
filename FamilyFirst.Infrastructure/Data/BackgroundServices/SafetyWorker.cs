using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class SafetyWorker : BackgroundService
{
    private static readonly TimeSpan LateAlertPollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PurgePollInterval     = TimeSpan.FromHours(24);
    private static readonly int      PurgeDays             = 30;

    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ILogger<SafetyWorker>  _logger;

    private DateTime _lastPurgeRun = DateTime.MinValue;

    public SafetyWorker(IServiceScopeFactory scopeFactory, ILogger<SafetyWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckLateAlertsAsync(stoppingToken);

                if (DateTime.UtcNow - _lastPurgeRun >= PurgePollInterval)
                {
                    await PurgeLocationHistoryAsync(stoppingToken);
                    _lastPurgeRun = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SafetyWorker: unhandled error.");
            }

            await Task.Delay(LateAlertPollInterval, stoppingToken);
        }
    }

    private async Task CheckLateAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var safetyRepo   = scope.ServiceProvider.GetRequiredService<ISafetyRepository>();
        var memberRepo   = scope.ServiceProvider.GetRequiredService<IFamilyMemberRepository>();
        var notifRepo    = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        // Match to the current minute so we only trigger once per minute window
        var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var dueZones    = await safetyRepo.GetZonesWithLateAlertDueAsync(currentTime, cancellationToken);

        foreach (var zone in dueZones)
        {
            Guid[] appliedMemberIds;
            try { appliedMemberIds = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(zone.AppliedMemberIdsJson) ?? Array.Empty<Guid>(); }
            catch { continue; }

            foreach (var memberId in appliedMemberIds)
            {
                var alreadyArrived = await safetyRepo.ArrivalAlertExistsTodayAsync(memberId, zone.Id, cancellationToken);
                if (alreadyArrived) continue;

                var alreadySent = await safetyRepo.LateAlertAlreadySentTodayAsync(memberId, zone.Id, cancellationToken);
                if (alreadySent) continue;

                var alert = new LocationAlert
                {
                    FamilyId         = zone.FamilyId,
                    FamilyMemberId   = memberId,
                    AlertType        = LocationAlertType.LateAlert,
                    ZoneId           = zone.Id,
                    ZoneNameSnapshot = zone.ZoneName,
                    TriggeredAt      = DateTime.UtcNow
                };
                await safetyRepo.AddAlertAsync(alert, cancellationToken);

                var parents = await memberRepo.ListActiveByFamilyAsync(zone.FamilyId, cancellationToken);
                var notifications = parents
                    .Where(m => m.Role is UserRole.Parent or UserRole.FamilyAdmin)
                    .Select(p => new Notification
                    {
                        RecipientUserId = p.UserId,
                        FamilyId        = zone.FamilyId,
                        Title           = $"Late alert — {zone.ZoneName}",
                        Body            = $"Expected arrival at {zone.ZoneName} by {zone.LateAlertTime} has not been confirmed.",
                        Channel         = NotificationChannel.Push,
                        Priority        = NotificationPriority.Normal,
                        IsRead          = false,
                        ScheduledFor    = DateTime.UtcNow
                    }).ToArray();

                await notifRepo.AddRangeAsync(notifications, cancellationToken);

                _logger.LogInformation(
                    "SafetyWorker: late alert sent. Zone={ZoneId} Member={MemberId} Family={FamilyId}.",
                    zone.Id, memberId, zone.FamilyId);
            }
        }
    }

    private async Task PurgeLocationHistoryAsync(CancellationToken cancellationToken)
    {
        using var scope     = _scopeFactory.CreateScope();
        var safetyRepo      = scope.ServiceProvider.GetRequiredService<ISafetyRepository>();
        var purgeOlderThan  = DateTime.UtcNow.AddDays(-PurgeDays);

        await safetyRepo.PurgeOldLocationHistoryAsync(purgeOlderThan, cancellationToken);

        _logger.LogInformation(
            "SafetyWorker: LocationHistory purged for records older than {PurgeDate:yyyy-MM-dd}. (DPDP Act 2023)",
            purgeOlderThan);
    }
}
