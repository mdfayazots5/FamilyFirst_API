using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Vault;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class DocumentVaultService : IDocumentVaultService
{
    private const int DefaultShareExpiryHours = 72;
    private const int MaxEmergencyDocuments = 5;
    private const int ExpiryWindowDays = 90;

    private readonly IVaultDocumentRepository _vaultRepository;
    private readonly IVaultStorageService _storageService;
    private readonly IFamilyMemberRepository _memberRepository;

    public DocumentVaultService(
        IVaultDocumentRepository vaultRepository,
        IVaultStorageService storageService,
        IFamilyMemberRepository memberRepository)
    {
        _vaultRepository = vaultRepository;
        _storageService = storageService;
        _memberRepository = memberRepository;
    }

    public async Task<VaultUploadUrlDto> GetUploadUrlAsync(
        Guid currentUserId,
        Guid familyId,
        VaultUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var category = (DocumentCategory)request.Category;

        return await _storageService.GenerateUploadUrlAsync(
            familyId,
            request.FileName,
            request.ContentType,
            category,
            cancellationToken);
    }

    public async Task<PaginatedList<DocumentDto>> ListDocumentsAsync(
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
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        DocumentCategory? categoryEnum = null;
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<DocumentCategory>(category, ignoreCase: true, out var parsed))
        {
            categoryEnum = parsed;
        }

        var (items, totalCount) = await _vaultRepository.ListAsync(
            familyId,
            categoryEnum,
            memberId,
            search,
            expiryStatus,
            dateFrom,
            dateTo,
            sortBy,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(MapToDocumentDto).ToList();
        return new PaginatedList<DocumentDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<DocumentDetailDto> GetDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var document = await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), documentId);

        return await MapToDetailDtoAsync(document, cancellationToken);
    }

    public async Task<DocumentDetailDto> GetDocumentByShareTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var shareLink = await _vaultRepository.GetShareLinkByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException("Share link not found or expired.");

        if (shareLink.IsRevoked || shareLink.ExpiresAt < DateTime.UtcNow)
        {
            throw new NotFoundException("Share link not found or expired.");
        }

        var document = shareLink.Document
            ?? await _vaultRepository.GetByIdAsync(shareLink.DocumentId, shareLink.FamilyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), shareLink.DocumentId);

        shareLink.LastAccessedAt = DateTime.UtcNow;
        await _vaultRepository.UpdateShareLinkAsync(shareLink, cancellationToken);

        return await MapToDetailDtoAsync(document, cancellationToken);
    }

    public async Task<DocumentDto> CreateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        CreateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        if (request.IsEmergencyPriority)
        {
            var emergencyCount = await _vaultRepository.CountEmergencyAsync(familyId, cancellationToken);
            if (emergencyCount >= MaxEmergencyDocuments)
            {
                throw new UnprocessableEntityException(
                    $"Family already has {MaxEmergencyDocuments} emergency-priority documents. Remove one before adding another.");
            }
        }

        var tagsJson = request.Tags is { Length: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.Tags)
            : null;

        var document = new VaultDocument
        {
            FamilyId = familyId,
            MemberId = request.MemberId,
            UploadedByUserId = currentUserId,
            DocumentName = request.DocumentName,
            Category = (DocumentCategory)request.Category,
            FileUrl = request.FileUrl,
            ExpiryDate = request.ExpiryDate,
            Tags = tagsJson,
            IsEmergencyPriority = request.IsEmergencyPriority,
            Visibility = request.Visibility.HasValue
                ? (DocumentVisibility)request.Visibility.Value
                : DocumentVisibility.ParentsOnly,
            VersionNumber = 1,
            IsCurrentVersion = true
        };

        var created = await _vaultRepository.AddAsync(document, cancellationToken);
        return MapToDocumentDto(created);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        UpdateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), documentId);

        var isFamilyAdmin = member.Role == UserRole.FamilyAdmin;
        if (!isFamilyAdmin && document.UploadedByUserId != currentUserId)
        {
            throw new ForbiddenAccessException();
        }

        if (request.IsEmergencyPriority == true && !document.IsEmergencyPriority)
        {
            var emergencyCount = await _vaultRepository.CountEmergencyAsync(familyId, cancellationToken);
            if (emergencyCount >= MaxEmergencyDocuments)
            {
                throw new UnprocessableEntityException(
                    $"Family already has {MaxEmergencyDocuments} emergency-priority documents.");
            }
        }

        if (request.DocumentName != null) document.DocumentName = request.DocumentName;
        if (request.ExpiryDate.HasValue) document.ExpiryDate = request.ExpiryDate;
        if (request.Tags != null) document.Tags = System.Text.Json.JsonSerializer.Serialize(request.Tags);
        if (request.Visibility.HasValue) document.Visibility = (DocumentVisibility)request.Visibility.Value;
        if (request.IsEmergencyPriority.HasValue) document.IsEmergencyPriority = request.IsEmergencyPriority.Value;

        if (!string.IsNullOrWhiteSpace(request.NewFileUrl))
        {
            var archivedVersion = new VaultDocumentVersion
            {
                DocumentId = document.Id,
                FamilyId = document.FamilyId,
                FileUrl = document.FileUrl,
                VersionNumber = document.VersionNumber,
                UploadedByUserId = document.UploadedByUserId,
                ArchivedAt = DateTime.UtcNow
            };

            await _vaultRepository.AddVersionAsync(archivedVersion, cancellationToken);

            document.FileUrl = request.NewFileUrl;
            document.VersionNumber++;
        }

        await _vaultRepository.UpdateAsync(document, cancellationToken);
        return MapToDocumentDto(document);
    }

    public async Task DeleteDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), documentId);

        var isFamilyAdmin = member.Role == UserRole.FamilyAdmin;
        if (!isFamilyAdmin && document.UploadedByUserId != currentUserId)
        {
            throw new ForbiddenAccessException();
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.PermanentDeleteAt = DateTime.UtcNow.AddDays(30);
        await _vaultRepository.UpdateAsync(document, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DocumentDto>> GetExpiringDocumentsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var documents = await _vaultRepository.ListExpiringAsync(familyId, ExpiryWindowDays, cancellationToken);
        return documents.Select(MapToDocumentDto).ToArray();
    }

    public async Task<IReadOnlyCollection<DocumentDto>> GetEmergencyDocumentsAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var documents = await _vaultRepository.ListEmergencyAsync(familyId, cancellationToken);
        return documents.Select(MapToDocumentDto).ToArray();
    }

    public async Task<ShareLinkDto> CreateShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), documentId);

        var expiryHours = request.ExpiryHours ?? DefaultShareExpiryHours;
        var allowDownload = request.AllowDownload ?? false;

        if (allowDownload && member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Only FamilyAdmin can create share links that permit download.");
        }

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var shareLink = new VaultShareLink
        {
            DocumentId = document.Id,
            FamilyId = familyId,
            CreatedByUserId = currentUserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            AllowDownload = allowDownload
        };

        var created = await _vaultRepository.AddShareLinkAsync(shareLink, cancellationToken);
        return MapToShareLinkDto(created);
    }

    public async Task RevokeShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var document = await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(VaultDocument), documentId);

        var shareLink = document.ShareLinks.FirstOrDefault(s => s.Id == shareLinkId)
            ?? throw new NotFoundException(nameof(VaultShareLink), shareLinkId);

        shareLink.IsRevoked = true;
        shareLink.RevokedAt = DateTime.UtcNow;
        await _vaultRepository.UpdateShareLinkAsync(shareLink, cancellationToken);
    }

    private async Task<FamilyMember> EnsureParentOrAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException();
        }

        return member;
    }

    private async Task<DocumentDetailDto> MapToDetailDtoAsync(
        VaultDocument document,
        CancellationToken cancellationToken)
    {
        var versions = await _vaultRepository.GetVersionHistoryAsync(document.Id, cancellationToken);

        var versionDtos = versions.Select(v => new DocumentVersionDto(
            v.Id,
            v.VersionNumber,
            v.FileUrl,
            v.UploadedByUserId,
            v.ArchivedAt)).ToArray();

        var shareLinkDtos = document.ShareLinks
            .Where(s => !s.IsDeleted && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .Select(MapToShareLinkDto)
            .ToArray();

        return new DocumentDetailDto(
            document.Id,
            document.DocumentName,
            (int)document.Category,
            document.Category.ToString(),
            (int)document.Visibility,
            document.MemberId,
            document.Member?.DisplayName ?? document.Member?.User?.FullName ?? string.Empty,
            document.UploadedByUserId,
            document.CreatedAt,
            document.ExpiryDate,
            ComputeExpiryStatus(document.ExpiryDate),
            document.FileUrl,
            null,
            ParseTags(document.Tags),
            document.IsEmergencyPriority,
            document.VersionNumber,
            versionDtos,
            shareLinkDtos);
    }

    private static DocumentDto MapToDocumentDto(VaultDocument doc) =>
        new(
            doc.Id,
            doc.DocumentName,
            (int)doc.Category,
            doc.Category.ToString(),
            doc.MemberId,
            doc.Member?.DisplayName ?? string.Empty,
            doc.UploadedByUserId,
            doc.CreatedAt,
            doc.ExpiryDate,
            ComputeExpiryStatus(doc.ExpiryDate),
            null,
            ParseTags(doc.Tags),
            doc.IsEmergencyPriority,
            doc.VersionNumber);

    private static ShareLinkDto MapToShareLinkDto(VaultShareLink link) =>
        new(
            link.Id,
            $"/vault/share/{link.Token}",
            link.ExpiresAt,
            link.AllowDownload,
            link.IsRevoked,
            link.LastAccessedAt,
            link.CreatedAt);

    private static string ComputeExpiryStatus(DateTime? expiryDate)
    {
        if (!expiryDate.HasValue) return "None";
        var daysLeft = (expiryDate.Value - DateTime.UtcNow).TotalDays;
        return daysLeft switch
        {
            < 0    => "Red",
            < 30   => "Red",
            < 90   => "Amber",
            _      => "Green"
        };
    }

    public async Task<VaultFamilySettingsDto> GetVaultSettingsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var settings = await _vaultRepository.GetVaultFamilySettingsAsync(familyId, cancellationToken);

        var mode = settings?.EmergencyAccessMode ?? Domain.Enums.EmergencyAccessMode.LoginRequired;
        return new VaultFamilySettingsDto(
            (int)mode,
            mode.ToString(),
            settings?.EmergencyPinHash != null);
    }

    public async Task<VaultFamilySettingsDto> UpdateVaultSettingsAsync(
        Guid currentUserId,
        Guid familyId,
        UpdateVaultFamilySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != Domain.Enums.UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Only FamilyAdmin can change vault emergency access settings.");
        }

        var mode = (Domain.Enums.EmergencyAccessMode)request.EmergencyAccessMode;

        string? pinHash = null;
        if (mode == Domain.Enums.EmergencyAccessMode.PinOnly && !string.IsNullOrWhiteSpace(request.EmergencyPin))
        {
            pinHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(request.EmergencyPin)));
        }

        var settings = new Domain.Entities.VaultFamilySettings
        {
            FamilyId             = familyId,
            EmergencyAccessMode  = mode,
            EmergencyPinHash     = pinHash
        };

        await _vaultRepository.UpsertVaultFamilySettingsAsync(settings, cancellationToken);

        return new VaultFamilySettingsDto((int)mode, mode.ToString(), pinHash != null);
    }

    private static string[] ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return Array.Empty<string>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(tagsJson) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
