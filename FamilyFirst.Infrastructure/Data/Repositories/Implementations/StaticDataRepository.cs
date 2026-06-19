using System.Data;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class StaticDataRepository : IStaticDataRepository
{
    private readonly string _connectionString;
    private readonly IMasterDataResolver _masterDataResolver;
    private readonly ILogger<StaticDataRepository> _logger;

    public StaticDataRepository(
        IConfiguration configuration,
        IMasterDataResolver masterDataResolver,
        ILogger<StaticDataRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
        _masterDataResolver = masterDataResolver;
        _logger = logger;
    }

    public async Task<string?> GetStoredProcedureNameAsync(
        string moduleCode,
        string methodName,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.uspGetStaticAPITemplateByModuleCode", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@ModuleCode", SqlDbType.NVarChar, 64).Value =
            string.IsNullOrWhiteSpace(moduleCode) ? DBNull.Value : moduleCode;
        command.Parameters.Add("@MethodName", SqlDbType.NVarChar, 256).Value =
            string.IsNullOrWhiteSpace(methodName) ? DBNull.Value : methodName;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is DBNull || result is null ? null : result.ToString();
    }

    public async Task<(IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Items, int TotalCount)> ExecuteSearchAsync(
        string spName,
        StaticSpParameters parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = BuildSpCommand(connection, spName, parameters);

        var items = new List<IReadOnlyDictionary<string, object?>>();
        var totalCount = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var colName  = reader.GetName(i);
                var colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);

                if (string.Equals(colName, "TotalCount", StringComparison.OrdinalIgnoreCase)
                    && colValue is int tc)
                {
                    totalCount = tc;
                }

                row[colName] = colValue;
            }

            items.Add(row);
        }

        _logger.LogDebug("[{Repo}] SP={SpName} returned {Rows} rows, TotalCount={TotalCount}",
            nameof(StaticDataRepository), spName, items.Count, totalCount);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task<IReadOnlyDictionary<string, object?>?> ExecuteCodeAsync(
        string spName,
        StaticSpParameters parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = BuildSpCommand(connection, spName, parameters);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return row;
    }

    // Delegates to IMasterDataResolver — uses MasterDataCodes.Family → uspGetMasterDataByCodeInternal
    // MasterDataCodes.Family.ToString() == "Family" → routes to tblFamily.FamilyId in SP
    public async Task<long> ResolveFamilyIdAsync(Guid familyGuid, CancellationToken cancellationToken)
    {
        if (familyGuid == Guid.Empty)
        {
            return 0L;
        }

        var result = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyGuid.ToString(),
            familyId: 0,
            cancellationToken);

        return result ?? 0L;
    }

    // Delegates to IMasterDataResolver — uses MasterDataCodes.User → uspGetMasterDataByCodeInternal
    // MasterDataCodes.User.ToString() == "User" → routes to tblUser.UserId in SP
    public async Task<long> ResolveUserIdAsync(Guid userGuid, CancellationToken cancellationToken)
    {
        if (userGuid == Guid.Empty)
        {
            return 0L;
        }

        var result = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.User,
            userGuid.ToString(),
            familyId: 0,
            cancellationToken);

        return result ?? 0L;
    }

    public async Task<IReadOnlyCollection<MasterDataItemDto>> GetMasterDataByCodeAsync(
        GetMastersRequest request,
        long familyId,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.uspGetMasterDataByCode", connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 15
        };

        command.Parameters.Add("@MasterDataCode", SqlDbType.NVarChar, 64).Value  = request.MasterDataCode;
        command.Parameters.Add("@Code",           SqlDbType.NVarChar, 64).Value  =
            string.IsNullOrWhiteSpace(request.Code) ? DBNull.Value : (object)request.Code.Trim();
        command.Parameters.Add("@SearchWord",     SqlDbType.NVarChar, 256).Value =
            string.IsNullOrWhiteSpace(request.SearchWord) ? DBNull.Value : (object)request.SearchWord.Trim();
        command.Parameters.Add("@IsPublished",    SqlDbType.Bit).Value            = request.IsPublished;
        command.Parameters.Add("@LanguageId",     SqlDbType.Int).Value            = request.LanguageId;
        command.Parameters.Add("@FamilyId",       SqlDbType.BigInt).Value         = familyId;

        var items = new List<MasterDataItemDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var idOrd   = reader.GetOrdinal("Id");
            var nameOrd = reader.GetOrdinal("Name");
            var codeOrd = reader.GetOrdinal("Code");
            var sortOrd = reader.GetOrdinal("SortOrder");

            items.Add(new MasterDataItemDto
            {
                Id        = reader.IsDBNull(idOrd)   ? Guid.Empty   : reader.GetGuid(idOrd),
                Name      = reader.IsDBNull(nameOrd) ? string.Empty : reader.GetString(nameOrd),
                Code      = reader.IsDBNull(codeOrd) ? string.Empty : reader.GetString(codeOrd),
                SortOrder = reader.IsDBNull(sortOrd) ? 0            : reader.GetInt32(sortOrd)
            });
        }

        _logger.LogDebug("[{Repo}] uspGetMasterDataByCode Code={Code} returned {Count} rows",
            nameof(StaticDataRepository), request.MasterDataCode, items.Count);

        return items.AsReadOnly();
    }

    private static SqlCommand BuildSpCommand(SqlConnection connection, string spName, StaticSpParameters p)
    {
        var command = new SqlCommand(spName, connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.Add("@FamilyId",   SqlDbType.BigInt).Value    = p.FamilyId;
        command.Parameters.Add("@UserId",      SqlDbType.BigInt).Value    = p.UserId;
        command.Parameters.Add("@RoleId",      SqlDbType.Int).Value       = p.RoleId;
        command.Parameters.Add("@Id",          SqlDbType.NVarChar, 64).Value  =
            string.IsNullOrWhiteSpace(p.Id) ? DBNull.Value : p.Id;
        command.Parameters.Add("@SearchWord",  SqlDbType.NVarChar, 256).Value =
            p.SearchWord is null ? DBNull.Value : p.SearchWord;
        command.Parameters.Add("@FromDate",    SqlDbType.DateTime2).Value =
            p.FromDate.HasValue ? p.FromDate.Value : DBNull.Value;
        command.Parameters.Add("@ToDate",      SqlDbType.DateTime2).Value =
            p.ToDate.HasValue ? p.ToDate.Value : DBNull.Value;
        command.Parameters.Add("@PageNumber",  SqlDbType.Int).Value       = p.PageNumber;
        command.Parameters.Add("@PageSize",    SqlDbType.Int).Value       = p.PageSize;
        command.Parameters.Add("@LanguageId",  SqlDbType.Int).Value       = p.LanguageId;

        return command;
    }
}
