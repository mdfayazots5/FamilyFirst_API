using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

/// <summary>
/// Daily physical purge of tblOtpRequest rows older than 24 hours (R2; founder-approved hard delete).
/// The 24-hour retention is far beyond the 5-minute OTP lifetime and the 60-minute request-quota window.
/// </summary>
public sealed class OtpCleanupWorker : BackgroundService
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private const int BatchSize = 5000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OtpCleanupWorker> _logger;

    public OtpCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<OtpCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removed = await PurgeAsync(DateTime.UtcNow - Retention, stoppingToken);
                _logger.LogInformation("OtpCleanupWorker: removed {Count} OTP rows older than {Hours}h.", removed, Retention.TotalHours);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OtpCleanupWorker: purge failed.");
            }

            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task<int> PurgeAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOtpStore>();
        var total = 0;
        int removed;
        do
        {
            removed = await store.PurgeBatchOlderThanAsync(cutoffUtc, BatchSize, cancellationToken);
            total += removed;
        }
        while (removed == BatchSize && !cancellationToken.IsCancellationRequested);

        return total;
    }
}
