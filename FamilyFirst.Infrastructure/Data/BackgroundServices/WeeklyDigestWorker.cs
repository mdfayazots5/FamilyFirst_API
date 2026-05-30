using System.Text.Json;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using FamilyFirst.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class WeeklyDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WeeklyDigestWorker> _logger;

    public WeeklyDigestWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WeeklyDigestWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = ResolveNextRunUtc(now);
            var delay = nextRunUtc - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                await ProcessWeeklyDigestAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Weekly digest worker failed.");
            }
        }
    }

    private async Task ProcessWeeklyDigestAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FamilyFirstDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        var weekStartDate = MostRecentMonday(DateOnly.FromDateTime(DateTime.UtcNow));
        var snapshotMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Auto-purge: archives older than 12 months; pillar history older than 13 months
        var archiveCutoff = weekStartDate.AddMonths(-12);
        var pillarCutoff = snapshotMonth.AddMonths(-13);
        await dbContext.WeeklyDigestArchives
            .Where(a => a.WeekStartDate < archiveCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ChildPillarScoreHistories
            .Where(h => h.SnapshotMonth < pillarCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var families = await dbContext.Families
            .Where(family => family.IsActive)
            .ToArrayAsync(cancellationToken);

        foreach (var family in families)
        {
            var digest = await reportService.GenerateWeeklyDigestAsync(
                family.Id,
                weekStartDate,
                cancellationToken);

            // Archive digest — skip if already written for this week (unique index is the safety net)
            var alreadyArchived = await dbContext.WeeklyDigestArchives
                .AnyAsync(a => a.FamilyId == family.Id && a.WeekStartDate == weekStartDate, cancellationToken);
            if (!alreadyArchived)
            {
                dbContext.WeeklyDigestArchives.Add(new WeeklyDigestArchive
                {
                    FamilyId          = family.Id,
                    WeekStartDate     = weekStartDate,
                    DigestContentJson = JsonSerializer.Serialize(digest),
                    GeneratedAt       = DateTime.UtcNow
                });
            }

            // Pillar snapshot — once per month per child; unique index prevents duplicates
            var children = await dbContext.ChildProfiles
                .Where(c => c.FamilyId == family.Id && !c.IsDeleted)
                .ToArrayAsync(cancellationToken);

            foreach (var child in children)
            {
                var snapshotExists = await dbContext.ChildPillarScoreHistories
                    .AnyAsync(h => h.ChildProfileId == child.Id && h.SnapshotMonth == snapshotMonth, cancellationToken);
                if (!snapshotExists)
                {
                    dbContext.ChildPillarScoreHistories.Add(new ChildPillarScoreHistory
                    {
                        ChildProfileId      = child.Id,
                        FamilyId            = family.Id,
                        SnapshotMonth       = snapshotMonth,
                        StudyScore          = child.StudyScore,
                        CleanlinessScore    = child.CleanlinessScore,
                        DisciplineScore     = child.DisciplineScore,
                        ScreenControlScore  = child.ScreenControlScore,
                        ResponsibilityScore = child.ResponsibilityScore
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var recipientUserIds = await dbContext.FamilyMembers
                .Where(member =>
                    member.FamilyId == family.Id
                    && member.IsActive
                    && (member.Role == UserRole.Parent || member.Role == UserRole.FamilyAdmin))
                .Select(member => member.UserId)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            if (recipientUserIds.Length == 0)
            {
                continue;
            }

            var requests = recipientUserIds
                .Select(recipientUserId => new CreateNotificationRequest
                {
                    FamilyId        = family.Id,
                    RecipientUserId = recipientUserId,
                    Title           = "Weekly family digest is ready",
                    Body            = BuildDigestBody(digest),
                    Priority        = NotificationPriority.High,
                    ReferenceType   = "WeeklyDigest",
                    ReferenceId     = family.Id,
                    DeepLinkPath    = $"/families/{family.Id}/reports/weekly-digest?weekStartDate={weekStartDate:yyyy-MM-dd}"
                })
                .ToArray();

            await notificationService.CreateManyAsync(requests, cancellationToken);
        }
    }

    private static DateTime ResolveNextRunUtc(DateTime utcNow)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)utcNow.DayOfWeek + 7) % 7;
        var nextRun = utcNow.Date.AddDays(daysUntilSunday).AddHours(19);

        return utcNow < nextRun ? nextRun : nextRun.AddDays(7);
    }

    private static string BuildDigestBody(Application.DTOs.Reports.WeeklyDigestDto digest)
    {
        return $"{digest.FamilyName}: {digest.Children.Count} child summaries, {digest.TotalFeedbackCount} feedback items, {digest.UpcomingEvents.Count} upcoming events.";
    }

    private static DateOnly MostRecentMonday(DateOnly referenceDate)
    {
        var dayOffset = ((int)referenceDate.DayOfWeek + 6) % 7;

        return referenceDate.AddDays(-dayOffset);
    }
}
