using FamilyFirst.Application.DTOs.StaticData;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IStaticDataRepository
{
    Task<string?> GetStoredProcedureNameAsync(string moduleCode, string methodName, CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Items, int TotalCount)> ExecuteSearchAsync(
        string spName,
        StaticSpParameters parameters,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>?> ExecuteCodeAsync(
        string spName,
        StaticSpParameters parameters,
        CancellationToken cancellationToken);

    Task<long> ResolveFamilyIdAsync(Guid familyGuid, CancellationToken cancellationToken);

    Task<long> ResolveUserIdAsync(Guid userGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Calls uspGetMasterDataByCode and maps rows to MasterDataItemDto.
    /// Returns empty collection if the MasterDataCode is unrecognised or has no rows.
    /// </summary>
    Task<IReadOnlyCollection<MasterDataItemDto>> GetMasterDataByCodeAsync(
        GetMastersRequest request,
        long familyId,
        CancellationToken cancellationToken);
}
