using System.Data;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

/// <summary>
/// Reusable BAL-internal GUID validator. Wraps uspGetMasterDataByCodeInternal.
///
/// Usage pattern in any service method:
///
///   // Step X — Validate TaskType GUID from UI
///   var taskTypeId = await _masterDataResolver.ResolveAsync(
///       MasterDataCodes.TaskType,
///       request.TaskTypeGuid);
///
///   if (taskTypeId is null)
///       throw new ValidationException(new() {{ "TaskTypeGuid", ["Invalid task type."] }});
///
///   // Now use taskTypeId (BIGINT) in the save SP call
///   await _taskRepository.CreateAsync(... taskTypeId.Value ...);
///
/// The enum name MUST match the MasterDataCode string in tblMasterData exactly.
/// MasterDataCodes.TaskType.ToString() == "TaskType" → SP parameter @MasterDataCode = 'TaskType'
/// </summary>
public sealed class MasterDataResolver : IMasterDataResolver
{
    private readonly string _connectionString;
    private readonly ILogger<MasterDataResolver> _logger;

    public MasterDataResolver(IConfiguration configuration, ILogger<MasterDataResolver> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _logger = logger;
    }

    public async Task<long?> ResolveAsync(
        MasterDataCodes code,
        string guid,
        long familyId = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            _logger.LogDebug("[{Method}] GUID is null/empty for code {Code}", nameof(ResolveAsync), code);
            return null;
        }

        var masterDataCode = code.ToString();

        _logger.LogDebug("[{Method}] Calling uspGetMasterDataByCodeInternal. Code={Code} Guid={Guid} FamilyId={FamilyId}",
            nameof(ResolveAsync), masterDataCode, guid, familyId);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.uspGetMasterDataByCodeInternal", connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 10
        };

        command.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar, 64).Value = masterDataCode;
        command.Parameters.Add("@GuidValue",       SqlDbType.NVarChar, 64).Value = guid.Trim();
        command.Parameters.Add("@LanguageId",       SqlDbType.Int).Value          = 1;
        command.Parameters.Add("@FamilyId",         SqlDbType.BigInt).Value       = familyId;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is DBNull || result is null)
        {
            _logger.LogDebug("[{Method}] GUID not found. Code={Code} Guid={Guid}", nameof(ResolveAsync), masterDataCode, guid);
            return null;
        }

        var id = Convert.ToInt64(result);
        _logger.LogDebug("[{Method}] Resolved. Code={Code} Guid={Guid} → MasterDataId={Id}",
            nameof(ResolveAsync), masterDataCode, guid, id);

        return id;
    }

    public async Task<IReadOnlyDictionary<MasterDataCodes, long?>> ResolveManyAsync(
        IReadOnlyCollection<(MasterDataCodes Code, string Guid)> items,
        long familyId = 0,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<MasterDataCodes, long?>(items.Count);

        foreach (var (code, guid) in items)
        {
            result[code] = await ResolveAsync(code, guid, familyId, cancellationToken);
        }

        return result;
    }
}
