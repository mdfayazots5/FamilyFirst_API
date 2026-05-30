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
}
