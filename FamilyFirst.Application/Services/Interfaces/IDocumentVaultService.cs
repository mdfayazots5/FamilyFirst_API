using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Vault;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IDocumentVaultService
{
    Task<VaultUploadUrlDto> GetUploadUrlAsync(
        Guid currentUserId,
        Guid familyId,
        VaultUploadUrlRequest request,
        CancellationToken cancellationToken);

    Task<PaginatedList<DocumentDto>> ListDocumentsAsync(
        Guid currentUserId,
        Guid familyId,
        string? category,
        Guid? memberId,
        string? search,
        string? expiryStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DocumentDetailDto> GetDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken);

    Task<DocumentDetailDto> GetDocumentByShareTokenAsync(
        string token,
        CancellationToken cancellationToken);

    Task<DocumentDto> CreateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        CreateVaultDocumentRequest request,
        CancellationToken cancellationToken);

    Task<DocumentDto> UpdateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        UpdateVaultDocumentRequest request,
        CancellationToken cancellationToken);

    Task DeleteDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentDto>> GetExpiringDocumentsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentDto>> GetEmergencyDocumentsAsync(
        Guid familyId,
        CancellationToken cancellationToken);

    Task<ShareLinkDto> CreateShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken);

    Task RevokeShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        Guid shareLinkId,
        CancellationToken cancellationToken);

    Task<VaultFamilySettingsDto> GetVaultSettingsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<VaultFamilySettingsDto> UpdateVaultSettingsAsync(
        Guid currentUserId,
        Guid familyId,
        UpdateVaultFamilySettingsRequest request,
        CancellationToken cancellationToken);
}
