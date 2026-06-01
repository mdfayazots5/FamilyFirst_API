using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class StaticDataService : IStaticDataService
{
    private readonly IStaticDataRepository _repository;
    private readonly IApiLogService _apiLogService;
    private readonly ILogger<StaticDataService> _logger;

    public StaticDataService(
        IStaticDataRepository repository,
        IApiLogService apiLogService,
        ILogger<StaticDataService> logger)
    {
        _repository    = repository;
        _apiLogService = apiLogService;
        _logger        = logger;
    }

    public async Task<StaticDataResponse> GetDataBySearchAsync(
        Guid currentUserId,
        Guid? currentFamilyId,
        string currentRole,
        StaticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(request);

        _logger.LogDebug("[{Method}] Step 1.0 — Start. ModuleCode={ModuleCode} MethodName={MethodName}",
            nameof(GetDataBySearchAsync), request.ModuleCode, request.MethodName);

        if (string.IsNullOrWhiteSpace(request.ModuleCode) || string.IsNullOrWhiteSpace(request.MethodName))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "ModuleCode", new[] { "ModuleCode and MethodName are required." } }
            });
        }

        _logger.LogDebug("[{Method}] Step 1.1 — Resolving SP name", nameof(GetDataBySearchAsync));
        var spName = await _repository.GetStoredProcedureNameAsync(
            request.ModuleCode, request.MethodName, cancellationToken);

        if (string.IsNullOrWhiteSpace(spName))
        {
            _logger.LogWarning("[{Method}] Step 1.2 — SP not found. ModuleCode={ModuleCode} MethodName={MethodName}",
                nameof(GetDataBySearchAsync), request.ModuleCode, request.MethodName);
            throw new NotFoundException(
                $"No stored procedure registered for ModuleCode='{request.ModuleCode}' MethodName='{request.MethodName}'.");
        }

        _logger.LogDebug("[{Method}] Step 2.0 — Resolving FamilyId and UserId", nameof(GetDataBySearchAsync));
        var familyId = currentFamilyId.HasValue
            ? await _repository.ResolveFamilyIdAsync(currentFamilyId.Value, cancellationToken)
            : 0L;

        var userId = await _repository.ResolveUserIdAsync(currentUserId, cancellationToken);
        var roleId = Enum.TryParse<UserRole>(currentRole, out var role) ? (int)role : 0;

        var spParams = new StaticSpParameters
        {
            FamilyId    = familyId,
            UserId      = userId,
            RoleId      = roleId,
            SearchWord  = request.SearchWord,
            FromDate    = request.FromDate,
            ToDate      = request.ToDate,
            PageNumber  = request.PageNumber < 1 ? 1 : request.PageNumber,
            PageSize    = request.PageSize is < 1 or > 100 ? 10 : request.PageSize,
            LanguageId  = request.LanguageId
        };

        _logger.LogDebug("[{Method}] Step 3.0 — Executing SP {SpName}", nameof(GetDataBySearchAsync), spName);
        var (items, totalCount) = await _repository.ExecuteSearchAsync(spName, spParams, cancellationToken);

        _logger.LogDebug("[{Method}] Step 3.1 — SP returned {Count} rows, TotalCount={TotalCount}",
            nameof(GetDataBySearchAsync), items.Count, totalCount);

        var totalPages = spParams.PageSize <= 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)spParams.PageSize);

        var response = new StaticDataResponse
        {
            Items           = items,
            TotalCount      = totalCount,
            PageNumber      = spParams.PageNumber,
            PageSize        = spParams.PageSize,
            TotalPages      = totalPages,
            HasNextPage     = spParams.PageNumber < totalPages,
            HasPreviousPage = spParams.PageNumber > 1
        };

        // Step 4.0 — Async API log (fire-and-forget via IApiLogService)
        _apiLogService.Log(
            methodName:       nameof(GetDataBySearchAsync),
            requestJson:      requestJson,
            responseJson:     JsonSerializer.Serialize(new { response.TotalCount, response.PageNumber, response.PageSize }),
            createdByUserId:  userId);

        return response;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetDataByCodeAsync(
        Guid currentUserId,
        Guid? currentFamilyId,
        string currentRole,
        StaticCodeRequest request,
        CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(request);

        _logger.LogDebug("[{Method}] Step 1.0 — Start. ModuleCode={ModuleCode} MethodName={MethodName} Id={Id}",
            nameof(GetDataByCodeAsync), request.ModuleCode, request.MethodName, request.Id);

        if (string.IsNullOrWhiteSpace(request.ModuleCode)
            || string.IsNullOrWhiteSpace(request.MethodName)
            || string.IsNullOrWhiteSpace(request.Id))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Id", new[] { "ModuleCode, MethodName, and Id (GUID) are required." } }
            });
        }

        _logger.LogDebug("[{Method}] Step 1.1 — Resolving SP name", nameof(GetDataByCodeAsync));
        var spName = await _repository.GetStoredProcedureNameAsync(
            request.ModuleCode, request.MethodName, cancellationToken);

        if (string.IsNullOrWhiteSpace(spName))
        {
            throw new NotFoundException(
                $"No stored procedure registered for ModuleCode='{request.ModuleCode}' MethodName='{request.MethodName}'.");
        }

        _logger.LogDebug("[{Method}] Step 2.0 — Resolving FamilyId and UserId", nameof(GetDataByCodeAsync));
        var familyId = currentFamilyId.HasValue
            ? await _repository.ResolveFamilyIdAsync(currentFamilyId.Value, cancellationToken)
            : 0L;

        var userId = await _repository.ResolveUserIdAsync(currentUserId, cancellationToken);
        var roleId = Enum.TryParse<UserRole>(currentRole, out var role) ? (int)role : 0;

        var spParams = new StaticSpParameters
        {
            FamilyId   = familyId,
            UserId     = userId,
            RoleId     = roleId,
            Id         = request.Id,
            LanguageId = request.LanguageId
        };

        _logger.LogDebug("[{Method}] Step 3.0 — Executing SP {SpName}", nameof(GetDataByCodeAsync), spName);
        var result = await _repository.ExecuteCodeAsync(spName, spParams, cancellationToken);

        if (result is null)
        {
            _logger.LogDebug("[{Method}] Step 3.1 — Record not found. Id={Id}", nameof(GetDataByCodeAsync), request.Id);
            throw new NotFoundException(
                $"Record not found for Id='{request.Id}' in ModuleCode='{request.ModuleCode}'.");
        }

        _logger.LogDebug("[{Method}] Step 3.2 — Record found", nameof(GetDataByCodeAsync));

        // Step 4.0 — Async API log (fire-and-forget via IApiLogService)
        _apiLogService.Log(
            methodName:      nameof(GetDataByCodeAsync),
            requestJson:     requestJson,
            responseJson:    JsonSerializer.Serialize(new { Id = request.Id, Found = true }),
            createdByUserId: userId);

        return result;
    }

    public async Task<GetMastersResponse> GetMastersAsync(
        Guid  currentUserId,
        Guid? currentFamilyId,
        GetMastersRequest request,
        CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(request);

        _logger.LogDebug("[{Method}] Step 1.0 — Start. MasterDataCode={Code}",
            nameof(GetMastersAsync), request.MasterDataCode);

        // Step 1 — Validate MasterDataCode is present and is a known enum value
        if (string.IsNullOrWhiteSpace(request.MasterDataCode))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "MasterDataCode", new[] { "MasterDataCode is required." } }
            });
        }

        if (!Enum.TryParse<MasterDataCodes>(request.MasterDataCode, ignoreCase: false, out _))
        {
            _logger.LogWarning("[{Method}] Step 1.1 — Unknown MasterDataCode: {Code}",
                nameof(GetMastersAsync), request.MasterDataCode);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "MasterDataCode", new[] { $"'{request.MasterDataCode}' is not a recognised master data category." } }
            });
        }

        // Step 2 — Resolve FamilyId INT PK from JWT GUID (0 for unscoped codes)
        _logger.LogDebug("[{Method}] Step 2.0 — Resolving FamilyId", nameof(GetMastersAsync));

        var familyId = currentFamilyId.HasValue
            ? await _repository.ResolveFamilyIdAsync(currentFamilyId.Value, cancellationToken)
            : 0L;

        // Step 3 — Call SP via repository
        _logger.LogDebug("[{Method}] Step 3.0 — Calling uspGetMasterDataByCode. FamilyId={FamilyId}",
            nameof(GetMastersAsync), familyId);

        var items = await _repository.GetMasterDataByCodeAsync(
            request with
            {
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize   = request.PageSize   < 1 ? 100 : request.PageSize > 500 ? 500 : request.PageSize
            },
            familyId,
            cancellationToken);

        _logger.LogDebug("[{Method}] Step 3.1 — SP returned {Count} rows", nameof(GetMastersAsync), items.Count);

        // Step 4 — Build response
        var response = new GetMastersResponse
        {
            Items      = items,
            TotalCount = items.Count
        };

        // Step 5 — Fire-and-forget API log
        _apiLogService.Log(
            methodName:  nameof(GetMastersAsync),
            requestJson: requestJson,
            responseJson: JsonSerializer.Serialize(new { request.MasterDataCode, response.TotalCount }));

        return response;
    }
}
