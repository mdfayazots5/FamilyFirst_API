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

        // LateAlertTime holds the family's local wall-clock time (time-of-day only). A zone is due while the
        // family-local time is inside [LateAlertTime, LateAlertTime + DueWindow); LateAlertAlreadySentTodayAsync
        // prevents repeats, and the window means a delayed loop iteration never skips the alert.
        var utcNow   = DateTime.UtcNow;
        var dueZones = (await safetyRepo.GetLateAlertEnabledZonesAsync(cancellationToken))
            .Where(z => IsLateAlertDue(z, utcNow))
            .ToArray();

        foreach (var zone in dueZones)
        {
            var familyId = zone.Family?.Id ?? Guid.Empty;
            var familyMembers = await memberRepo.ListActiveByFamilyAsync(familyId, cancellationToken);
            Guid[] appliedMemberIds;
            try { appliedMemberIds = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(zone.AppliedMemberIdsJson) ?? Array.Empty<Guid>(); }
            catch { continue; }

            foreach (var memberId in appliedMemberIds)
            {
                var alreadyArrived = await safetyRepo.ArrivalAlertExistsTodayAsync(memberId, zone.Id, cancellationToken);
                if (alreadyArrived) continue;

                var alreadySent = await safetyRepo.LateAlertAlreadySentTodayAsync(memberId, zone.Id, cancellationToken);
                if (alreadySent) continue;

                var member = familyMembers.FirstOrDefault(m => m.Id == memberId);
                if (member is null) continue;

                var alert = new LocationAlert
                {
                    FamilyId         = zone.FamilyId,
                    FamilyMemberId   = member.InternalId,
                    AlertType        = LocationAlertType.LateAlert,
                    ZoneId           = zone.InternalId,
                    ZoneNameSnapshot = zone.ZoneName,
                    TriggeredAt      = DateTime.UtcNow
                };
                await safetyRepo.AddAlertAsync(alert, cancellationToken);

                var notifications = familyMembers
                    .Where(m => m.Role is UserRole.Parent or UserRole.FamilyAdmin)
                    .Select(p => new Notification
                    {
                        RecipientUserId = p.UserId,
                        FamilyId        = zone.FamilyId,
                        Title           = $"Late alert — {zone.ZoneName}",
                        Body            = $"Expected arrival at {zone.ZoneName} by {zone.LateAlertTime:HH:mm} has not been confirmed.",
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

    public static readonly TimeSpan LateAlertDueWindow = TimeSpan.FromMinutes(10);

    public static bool IsLateAlertDue(SafeZone zone, DateTime utcNow)
    {
        if (!zone.LateAlertEnabled || !zone.LateAlertTime.HasValue) return false;

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            ResolveTimeZone(zone.Family?.TimezoneId));
        var elapsed = localNow.TimeOfDay - zone.LateAlertTime.Value.TimeOfDay;
        return elapsed >= TimeSpan.Zero && elapsed < LateAlertDueWindow;
    }

    public static TimeZoneInfo ResolveTimeZone(string? timezoneId)
    {
        var id = string.IsNullOrWhiteSpace(timezoneId) ? "Asia/Kolkata" : timezoneId;
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone)) return zone;
        return TimeZoneInfo.Utc;
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
