using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class VaultExpiryWorker : BackgroundService
{
    private static readonly int[] InsuranceThresholds = { 90, 30, 14, 3 };
    private static readonly int[] IdentityThresholds  = { 90, 30, 7 };
    private static readonly int[] DefaultThresholds   = { 30 };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VaultExpiryWorker> _logger;

    public VaultExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<VaultExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiryRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VaultExpiryWorker: unhandled error during expiry evaluation.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessExpiryRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var vaultRepository       = scope.ServiceProvider.GetRequiredService<IVaultDocumentRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var memberRepository      = scope.ServiceProvider.GetRequiredService<IFamilyMemberRepository>();

        var allThresholds = InsuranceThresholds
            .Union(IdentityThresholds)
            .Union(DefaultThresholds)
            .Distinct()
            .OrderByDescending(t => t)
            .ToArray();

        foreach (var threshold in allThresholds)
        {
            var documents = await vaultRepository.GetDocumentsDueForReminderAsync(threshold, cancellationToken);

            foreach (var doc in documents)
            {
                if (!ThresholdApplies(doc.Category, threshold)) continue;

                var alreadySent = await vaultRepository.ReminderAlreadySentAsync(doc.Id, threshold, cancellationToken);
                if (alreadySent) continue;

                var familyId = doc.Family.Id;
                var parents = await memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
                var recipients = parents
                    .Where(m => m.Role is UserRole.Parent or UserRole.FamilyAdmin)
                    .ToArray();

                if (recipients.Length == 0) continue;

                var isUrgent  = threshold <= 3 && doc.Category == DocumentCategory.Insurance;
                var title     = isUrgent
                    ? $"URGENT: {doc.DocumentName} expires today!"
                    : $"Reminder: {doc.DocumentName} expires in {threshold} days";
                var body      = $"Document '{doc.DocumentName}' expires on {doc.ExpiryDate:dd MMM yyyy}.";
                var priority  = isUrgent ? NotificationPriority.Urgent : NotificationPriority.Normal;

                var notifications = recipients.Select(m => new Notification
                {
                    RecipientUserId = m.UserId,
                    FamilyId        = doc.FamilyId,
                    Title           = title,
                    Body            = body,
                    Channel         = NotificationChannel.Push,
                    Priority        = priority,
                    IsRead          = false,
                    ScheduledFor    = DateTime.UtcNow,
                    ReferenceType   = "VaultDocument",
                    ReferenceId     = doc.InternalId
                }).ToArray();

                await notificationRepository.AddRangeAsync(notifications, cancellationToken);
                await vaultRepository.RecordReminderSentAsync(doc.Id, familyId, threshold, cancellationToken);

                _logger.LogInformation(
                    "VaultExpiryWorker: reminder queued. Document={DocumentId} Threshold={Threshold}d Family={FamilyId}.",
                    doc.Id, threshold, doc.FamilyId);
            }
        }
    }

    private static bool ThresholdApplies(DocumentCategory category, int threshold)
    {
        var applicable = category switch
        {
            DocumentCategory.Insurance => InsuranceThresholds,
            DocumentCategory.Identity  => IdentityThresholds,
            _                          => DefaultThresholds
        };
        return applicable.Contains(threshold);
    }
}
