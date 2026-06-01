using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

/// <summary>
/// Reusable BAL-internal GUID validator.
/// Given a MasterDataCodes enum value and the GUID sent from the UI,
/// validates the GUID is a real active record and returns the INT PK.
///
/// The INT PK is then passed to save stored procedures.
/// UI never sees INT PKs — it only ever sends and receives GUIDs.
///
/// Wraps uspGetMasterDataByCodeInternal.
/// Returns null if the GUID is invalid, not found, or family mismatch.
/// Caller sets the appropriate error code (FamilyFirstErrorCode.Invalid_MasterData = 23).
/// </summary>
public interface IMasterDataResolver
{
    /// <summary>
    /// Validates a GUID for the given master data category.
    /// </summary>
    /// <param name="code">
    /// Master data category enum — e.g. MasterDataCodes.TaskType.
    /// Enum.ToString() is used as the @MasterDataCode parameter to the SP.
    /// </param>
    /// <param name="guid">
    /// The GUID string sent from the UI (from a previous GetMasterDataByCode response).
    /// </param>
    /// <param name="familyId">
    /// BIGINT FamilyId for scoped validation (FamilyMember, ChildProfile, etc.).
    /// Pass 0 for unscoped lookups (Role, TaskType, Plan, etc.).
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The INT PK (MasterDataId) if valid, or null if the GUID is invalid/unauthorised.
    /// </returns>
    Task<long?> ResolveAsync(
        MasterDataCodes code,
        string guid,
        long familyId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple GUIDs in one method call.
    /// Returns a dictionary of code → INT PK.
    /// Any entry with null value means that GUID was invalid.
    /// </summary>
    Task<IReadOnlyDictionary<MasterDataCodes, long?>> ResolveManyAsync(
        IReadOnlyCollection<(MasterDataCodes Code, string Guid)> items,
        long familyId = 0,
        CancellationToken cancellationToken = default);
}
