using FamilyFirst.Application.DTOs.StaticData;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IStaticDataService
{
    Task<StaticDataResponse> GetDataBySearchAsync(
        Guid currentUserId,
        Guid? currentFamilyId,
        string currentRole,
        StaticSearchRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, object?>?> GetDataByCodeAsync(
        Guid currentUserId,
        Guid? currentFamilyId,
        string currentRole,
        StaticCodeRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns master data items for a given MasterDataCode category.
    /// Calls uspGetMasterDataByCode. Returns GUIDs only — no INT PKs.
    /// FamilyId-scoped codes (FamilyMember, ChildProfile, etc.) are filtered by the caller's familyId.
    /// </summary>
    Task<GetMastersResponse> GetMastersAsync(
        Guid  currentUserId,
        Guid? currentFamilyId,
        GetMastersRequest request,
        CancellationToken cancellationToken);
}
