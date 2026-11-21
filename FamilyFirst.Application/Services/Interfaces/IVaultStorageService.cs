using FamilyFirst.Application.DTOs.Vault;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IVaultStorageService
{
    Task<VaultUploadUrlDto> GenerateUploadUrlAsync(
        Guid familyId,
        string fileName,
        string contentType,
        DocumentCategory category,
        CancellationToken cancellationToken);

    /// <summary>
    /// True when <paramref name="fileUrl"/> is an object URL inside this family's vault prefix
    /// (<c>family/{familyId}/vault/</c>) of the configured bucket. Client-supplied FileUrls must pass this check.
    /// When storage is not configured (local development) any absolute https URL is accepted.
    /// </summary>
    bool IsFamilyObjectUrl(Guid familyId, string fileUrl);

    /// <summary>
    /// Returns a short-lived presigned GET URL for a stored vault object URL, so the bucket can stay private.
    /// URLs outside the configured bucket (or when storage is not configured) are returned unchanged.
    /// </summary>
    string CreateReadUrl(string storedFileUrl);
}
