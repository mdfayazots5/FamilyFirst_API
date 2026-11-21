using System.Data;
using System.Text.RegularExpressions;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using FamilyFirst.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.BackgroundServices;

public sealed class CacheWarmupService(
    IConfiguration configuration, IServiceScopeFactory scopeFactory, IMemoryCache cache, StartupCache startupCache,
    ILogger<CacheWarmupService> logger) : IHostedService
{
    // R5: configuration (error codes, permissions, API methods, regex) is reloaded on this interval, so DB edits to
    // tblRegularExpression / tblAPIMethod take effect without a restart. Error/permission entries also expire after
    // StartupCache.Lifetime and fall back to their stored procedures.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);
    private readonly CancellationTokenSource _stopping = new();
    private Task? _refreshLoop;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await WarmAsync(cancellationToken, includeMasterData: true);
        _refreshLoop = RefreshLoopAsync(_stopping.Token);
    }

    private async Task RefreshLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(RefreshInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
            await WarmAsync(stoppingToken, includeMasterData: false);
        }
    }

    private async Task WarmAsync(CancellationToken cancellationToken, bool includeMasterData)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        IReadOnlyList<string> masterCodes;
        try
        {
            var (errors, permissions, methods, patterns, codes) = await LoadConfigurationAsync(timeout.Token);
            masterCodes = codes;
            logger.LogInformation(
                "Startup cache loaded {Errors} error codes, {Permissions} role permissions, {Methods} API methods and {Patterns} regex rules.",
                errors, permissions, methods, patterns);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Any failure (SQL, timeout, unexpected data) degrades to the stored procedures and validator defaults;
            // the host must never fail to start because of the cache.
            logger.LogWarning(ex, "Startup cache configuration warmup failed. Stored procedures and validator defaults remain available.");
            return;
        }

        if (!includeMasterData)
            return;

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStaticDataRepository>();
        var lookupCount = 0;
        var rowCount = 0;
        foreach (var code in masterCodes)
        {
            try
            {
                var rows = await repository.GetMasterDataByCodeAsync(new GetMastersRequest { MasterDataCode = code }, 0, timeout.Token);
                lookupCount++;
                rowCount += rows.Count;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Static master data warmup stopped at {Code}. Uncached lookups use the stored procedure.", code);
                break;
            }
        }
        logger.LogInformation("Startup cache loaded {Lookups} static master lookups with {Rows} rows.", lookupCount, rowCount);
    }

    private async Task<(int Errors, int Permissions, int Methods, int Patterns, IReadOnlyList<string> MasterCodes)> LoadConfigurationAsync(CancellationToken token)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("SqlServer"));
        await connection.OpenAsync(token);
        await using var command = new SqlCommand("dbo.uspGetStartupCacheData", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 5
        };
        await using var reader = await command.ExecuteReaderAsync(token);
        var generation = startupCache.Generation;
        var errors = 0;
        while (await reader.ReadAsync(token))
        {
            startupCache.Remember(generation, StartupCache.Error(reader.GetInt32(0), reader.GetInt32(1)), (reader.GetInt32(2), reader.GetString(3)));
            errors++;
        }

        await reader.NextResultAsync(token);
        var permissions = new Dictionary<(long Role, long Module), HashSet<long>>();
        while (await reader.ReadAsync(token))
        {
            var key = (reader.GetInt64(0), reader.GetInt64(1));
            if (!permissions.TryGetValue(key, out var grants))
                permissions.Add(key, grants = []);
            grants.Add(reader.GetInt64(2));
        }
        foreach (var role in Enum.GetValues<UserRole>())
        foreach (var module in Enum.GetValues<FamilyFirstModule>())
            permissions.TryAdd(((long)role, (long)module), []);
        foreach (var (key, grants) in permissions)
            startupCache.Remember(generation, StartupCache.Permission(key.Role, key.Module), grants);

        await reader.NextResultAsync(token);
        var methods = 0;
        while (await reader.ReadAsync(token))
        {
            cache.Set(StartupCache.Method(reader.GetString(1)), reader.GetInt64(0));
            methods++;
        }

        await reader.NextResultAsync(token);
        var patterns = 0;
        while (await reader.ReadAsync(token))
        {
            var method = reader.GetInt64(0);
            var field = reader.GetString(1);
            try
            {
                var regex = new Regex(reader.GetString(2), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                cache.Set(StartupCache.Pattern(method, field), regex);
                patterns++;
            }
            catch (ArgumentException)
            {
                logger.LogWarning("Invalid regex for API method {MethodId}, field {Field}. The validator default will be used.", method, field);
            }
        }
        await reader.NextResultAsync(token);
        var masterCodes = new List<string>();
        while (await reader.ReadAsync(token))
        {
            var code = reader.GetString(0);
            if (StaticMasterData.Contains(code))
                masterCodes.Add(code);
        }
        return (errors, permissions.Values.Sum(grants => grants.Count), methods, patterns, masterCodes);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync();
        if (_refreshLoop is not null)
            await Task.WhenAny(_refreshLoop, Task.Delay(Timeout.Infinite, cancellationToken));
        _stopping.Dispose();
    }
}
