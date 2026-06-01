using System.Data;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

/// <summary>
/// Fire-and-forget implementation of IApiLogService.
/// Log() returns immediately — the actual DB insert runs in the background via Task.Run.
/// Never throws — all exceptions are swallowed so a logging failure never breaks the response.
/// Wraps: uspInsertAPILog
/// </summary>
public sealed class ApiLogService : IApiLogService
{
    private readonly string _connectionString;
    private readonly ILogger<ApiLogService> _logger;

    public ApiLogService(IConfiguration configuration, ILogger<ApiLogService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _logger = logger;
    }

    public void Log(
        string methodName,
        string? requestJson,
        string? responseJson,
        long apiMethodId = 0,
        long createdByUserId = 0,
        string? ipAddress = null,
        string? createdBy = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await InsertAsync(
                    methodName, requestJson, responseJson,
                    apiMethodId, createdByUserId,
                    ipAddress, createdBy);
            }
            catch (Exception ex)
            {
                // Fire-and-forget — log failure must never crash the response
                _logger.LogWarning(ex, "[ApiLogService] Failed to insert API log for method {Method}", methodName);
            }
        });
    }

    private async Task InsertAsync(
        string methodName,
        string? requestJson,
        string? responseJson,
        long apiMethodId,
        long createdByUserId,
        string? ipAddress,
        string? createdBy)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("dbo.uspInsertAPILog", connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 10
        };

        command.Parameters.Add("@APIMethodId",     SqlDbType.BigInt).Value       = apiMethodId > 0 ? apiMethodId : DBNull.Value;
        command.Parameters.Add("@MethodName",      SqlDbType.NVarChar, 256).Value = methodName;
        command.Parameters.Add("@RequestJSON",     SqlDbType.NVarChar, -1).Value  = requestJson    is null ? DBNull.Value : requestJson;
        command.Parameters.Add("@ResponseJSON",    SqlDbType.NVarChar, -1).Value  = responseJson   is null ? DBNull.Value : responseJson;
        command.Parameters.Add("@Token",           SqlDbType.NVarChar, 2048).Value = DBNull.Value;
        command.Parameters.Add("@CreatedByUserId", SqlDbType.BigInt).Value        = createdByUserId > 0 ? createdByUserId : DBNull.Value;
        command.Parameters.Add("@IPAddress",       SqlDbType.NVarChar, 64).Value  = ipAddress      is null ? DBNull.Value : ipAddress;
        command.Parameters.Add("@CreatedBy",       SqlDbType.NVarChar, 128).Value = createdBy      is null ? DBNull.Value : createdBy;

        await command.ExecuteNonQueryAsync();

        _logger.LogDebug("[ApiLogService] Log inserted for method {Method}", methodName);
    }
}
