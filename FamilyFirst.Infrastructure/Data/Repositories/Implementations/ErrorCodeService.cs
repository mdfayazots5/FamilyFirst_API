using System.Data;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

/// <summary>
/// Reads error/success messages from tblErrorCode via uspGetErrorCodeById.
/// Wraps: uspGetErrorCodeById
///
/// Usage in any service method finally block:
///   var message = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Permission_Denied);
///   throw new ForbiddenAccessException(message);
///
///   -- OR for success response --
///   var message = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Success);
///   return ApiResponse{T}.Success(data, message);
/// </summary>
public sealed class ErrorCodeService : IErrorCodeService
{
    private readonly string _connectionString;
    private readonly ILogger<ErrorCodeService> _logger;

    public ErrorCodeService(IConfiguration configuration, ILogger<ErrorCodeService> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
        _logger = logger;
    }

    public async Task<string> GetMessageAsync(
        FamilyFirstErrorCode code,
        int languageId = 1,
        CancellationToken cancellationToken = default)
    {
        var (_, message) = await GetAsync(code, languageId, cancellationToken);
        return message;
    }

    public async Task<(int ReturnCode, string ReturnMessage)> GetAsync(
        FamilyFirstErrorCode code,
        int languageId = 1,
        CancellationToken cancellationToken = default)
    {
        var errorCodeInt = (int)code;

        _logger.LogDebug("[{Service}] Step 1 — Calling uspGetErrorCodeById. Code={Code} Language={Lang}",
            nameof(ErrorCodeService), errorCodeInt, languageId);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.uspGetErrorCodeById", connection)
            {
                CommandType    = CommandType.StoredProcedure,
                CommandTimeout = 5
            };

            command.Parameters.Add("@ErrorCode",  SqlDbType.Int).Value = errorCodeInt;
            command.Parameters.Add("@LanguageId", SqlDbType.Int).Value = languageId;

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var returnCode    = reader.IsDBNull(reader.GetOrdinal("ReturnCode"))
                    ? errorCodeInt
                    : reader.GetInt32(reader.GetOrdinal("ReturnCode"));

                var returnMessage = reader.IsDBNull(reader.GetOrdinal("ReturnMessage"))
                    ? FallbackMessage(errorCodeInt)
                    : reader.GetString(reader.GetOrdinal("ReturnMessage"));

                _logger.LogDebug("[{Service}] Step 2 — Message retrieved. Code={Code} Message={Msg}",
                    nameof(ErrorCodeService), errorCodeInt, returnMessage);

                return (returnCode, returnMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Service}] Failed to get error code {Code}", nameof(ErrorCodeService), errorCodeInt);
        }

        return (errorCodeInt, FallbackMessage(errorCodeInt));
    }

    private static string FallbackMessage(int code) =>
        code == 0 ? "Success" : $"A technical error occurred. Code: {code}";
}
