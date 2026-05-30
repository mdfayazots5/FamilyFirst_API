using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IVaultDocumentRepository
{
    Task<VaultDocument?> GetByIdAsync(Guid documentId, Guid familyId, CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<VaultDocument> Items, int TotalCount)> ListAsync(
        Guid familyId,
        DocumentCategory? category,
        Guid? memberId,
        string? search,
        string? expiryStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VaultDocument>> ListExpiringAsync(
        Guid familyId,
        int withinDays,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VaultDocument>> ListEmergencyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<int> CountEmergencyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<VaultDocument> AddAsync(VaultDocument document, CancellationToken cancellationToken);

    Task UpdateAsync(VaultDocument document, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VaultDocumentVersion>> GetVersionHistoryAsync(Guid documentId, CancellationToken cancellationToken);

    Task AddVersionAsync(VaultDocumentVersion version, CancellationToken cancellationToken);

    Task<VaultShareLink?> GetShareLinkByTokenAsync(string token, CancellationToken cancellationToken);

    Task<VaultShareLink> AddShareLinkAsync(VaultShareLink shareLink, CancellationToken cancellationToken);

    Task UpdateShareLinkAsync(VaultShareLink shareLink, CancellationToken cancellationToken);

    Task<bool> ReminderAlreadySentAsync(Guid documentId, int thresholdDays, CancellationToken cancellationToken);

    Task RecordReminderSentAsync(Guid documentId, Guid familyId, int thresholdDays, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VaultDocument>> GetDocumentsDueForReminderAsync(
        int thresholdDays,
        CancellationToken cancellationToken);

    Task<VaultFamilySettings?> GetVaultFamilySettingsAsync(Guid familyId, CancellationToken cancellationToken);

    Task UpsertVaultFamilySettingsAsync(VaultFamilySettings settings, CancellationToken cancellationToken);
}
