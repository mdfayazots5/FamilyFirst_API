using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Vault;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class DocumentVaultService : IDocumentVaultService
{
    private const int DefaultShareExpiryHours = 72;
    private const int MaxEmergencyDocuments = 5;
    private const int ExpiryWindowDays = 90;

    private readonly IVaultDocumentRepository _vaultRepository;
    private readonly IVaultStorageService _storageService;
    private readonly IFamilyMemberRepository _memberRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public DocumentVaultService(
        IVaultDocumentRepository vaultRepository,
        IVaultStorageService storageService,
        IFamilyMemberRepository memberRepository,
        IApiLogService apiLogService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _vaultRepository = vaultRepository;
        _storageService = storageService;
        _memberRepository = memberRepository;
        _apiLogService = apiLogService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<VaultUploadUrlDto> GetUploadUrlAsync(
        Guid currentUserId,
        Guid familyId,
        VaultUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var category = (DocumentCategory)request.Category;

        var response = await _storageService.GenerateUploadUrlAsync(
            familyId,
            request.FileName,
            request.ContentType,
            category,
            cancellationToken);
        LogApiCall(nameof(GetUploadUrlAsync), new { currentUserId, familyId, request.FileName, request.Category }, new { response.FileUrl, response.ExpiresAtUtc });
        return response;
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
        var response = new PaginatedList<DocumentDto>(dtos, totalCount, page, pageSize);
        LogApiCall(nameof(ListDocumentsAsync), new { currentUserId, familyId, category, memberId, search, expiryStatus, dateFrom, dateTo, sortBy, page, pageSize }, new { response.TotalCount });
        return response;
    }

    public async Task<DocumentDetailDto> GetDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var document = await GetDocumentOrThrowAsync(documentId, familyId, cancellationToken);
        var response = await MapToDetailDtoAsync(document, cancellationToken);
        LogApiCall(nameof(GetDocumentAsync), new { currentUserId, familyId, documentId }, new { response.DocumentId });
        return response;
    }

    public async Task<DocumentDetailDto> GetDocumentByShareTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var shareLink = await _vaultRepository.GetShareLinkByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (shareLink.IsRevoked || shareLink.ExpiresAt < DateTime.UtcNow)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        var document = shareLink.VaultDocument
            ?? await _vaultRepository.GetByIdAsync(shareLink.VaultDocument?.Id ?? Guid.Empty, shareLink.Family?.Id ?? Guid.Empty, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        shareLink.LastAccessedAt = DateTime.UtcNow;
        await _vaultRepository.UpdateShareLinkAsync(shareLink, cancellationToken);

        var response = await MapToDetailDtoAsync(document, cancellationToken);
        LogApiCall(nameof(GetDocumentByShareTokenAsync), new { token }, new { response.DocumentId });
        return response;
    }

    public async Task<DocumentDto> CreateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        CreateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var creatingMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

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

        var resolvedMemberId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.FamilyMember,
            request.MemberId.ToString(),
            creatingMember.FamilyId,
            cancellationToken);

        if (!resolvedMemberId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(nameof(request.MemberId), cancellationToken);
        }

        var document = new VaultDocument
        {
            FamilyId = creatingMember?.FamilyId ?? 0L,
            FamilyMemberId = resolvedMemberId.Value,
            UploadedByUserId = creatingMember?.UserId ?? 0L,
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
        var createdDocument = await _vaultRepository.GetByIdAsync(created.Id, familyId, cancellationToken) ?? created;
        var response = MapToDocumentDto(createdDocument);
        LogApiCall(nameof(CreateDocumentAsync), new { currentUserId, familyId, request.DocumentName, request.MemberId, request.Category, request.IsEmergencyPriority }, new { response.DocumentId });
        return response;
    }

    public async Task<DocumentDto> UpdateDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        UpdateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await GetDocumentOrThrowAsync(documentId, familyId, cancellationToken);

        var isFamilyAdmin = member.Role == UserRole.FamilyAdmin;
        if (!isFamilyAdmin && document.UploadedByUser?.Id != currentUserId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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
                VaultDocumentId = document.InternalId,
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
        var updatedDocument = await _vaultRepository.GetByIdAsync(document.Id, familyId, cancellationToken) ?? document;
        var response = MapToDocumentDto(updatedDocument);
        LogApiCall(nameof(UpdateDocumentAsync), new { currentUserId, familyId, documentId }, new { response.DocumentId, response.VersionNumber });
        return response;
    }

    public async Task DeleteDocumentAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await GetDocumentOrThrowAsync(documentId, familyId, cancellationToken);

        var isFamilyAdmin = member.Role == UserRole.FamilyAdmin;
        if (!isFamilyAdmin && document.UploadedByUser?.Id != currentUserId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.PermanentDeleteAt = DateTime.UtcNow.AddDays(30);
        await _vaultRepository.UpdateAsync(document, cancellationToken);
        LogApiCall(nameof(DeleteDocumentAsync), new { currentUserId, familyId, documentId }, new { Success = true });
    }

    public async Task<IReadOnlyCollection<DocumentDto>> GetExpiringDocumentsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var documents = await _vaultRepository.ListExpiringAsync(familyId, ExpiryWindowDays, cancellationToken);
        var response = documents.Select(MapToDocumentDto).ToArray();
        LogApiCall(nameof(GetExpiringDocumentsAsync), new { currentUserId, familyId }, new { Count = response.Length });
        return response;
    }

    public async Task<IReadOnlyCollection<DocumentDto>> GetEmergencyDocumentsAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var documents = await _vaultRepository.ListEmergencyAsync(familyId, cancellationToken);
        var response = documents.Select(MapToDocumentDto).ToArray();
        LogApiCall(nameof(GetEmergencyDocumentsAsync), new { familyId }, new { Count = response.Length });
        return response;
    }

    public async Task<ShareLinkDto> CreateShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var document = await GetDocumentOrThrowAsync(documentId, familyId, cancellationToken);

        var expiryHours = request.ExpiryHours ?? DefaultShareExpiryHours;
        var allowDownload = request.AllowDownload ?? false;

        if (allowDownload && member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var sharingMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var shareLink = new VaultShareLink
        {
            VaultDocumentId = document.InternalId,
            FamilyId = sharingMember?.FamilyId ?? 0L,
            CreatedByUserId = sharingMember?.UserId ?? 0L,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            AllowDownload = allowDownload
        };

        var created = await _vaultRepository.AddShareLinkAsync(shareLink, cancellationToken);
        var response = MapToShareLinkDto(created);
        LogApiCall(nameof(CreateShareLinkAsync), new { currentUserId, familyId, documentId, expiryHours, allowDownload }, new { response.ShareLinkId, response.ExpiresAt });
        return response;
    }

    public async Task RevokeShareLinkAsync(
        Guid currentUserId,
        Guid familyId,
        Guid documentId,
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var document = await GetDocumentOrThrowAsync(documentId, familyId, cancellationToken);

        var shareLink = document.ShareLinks.FirstOrDefault(s => s.Id == shareLinkId)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        shareLink.IsRevoked = true;
        shareLink.RevokedAt = DateTime.UtcNow;
        await _vaultRepository.UpdateShareLinkAsync(shareLink, cancellationToken);
        LogApiCall(nameof(RevokeShareLinkAsync), new { currentUserId, familyId, documentId, shareLinkId }, new { Success = true });
    }

    private async Task<FamilyMember> EnsureParentOrAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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
            v.UploadedByUser?.Id ?? Guid.Empty,
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
            document.FamilyMember?.Id ?? Guid.Empty,
            document.FamilyMember?.DisplayName ?? document.FamilyMember?.User?.FullName ?? string.Empty,
            document.UploadedByUser?.Id ?? Guid.Empty,
            document.DateCreated,
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
            doc.FamilyMember?.Id ?? Guid.Empty,
            doc.FamilyMember?.DisplayName ?? string.Empty,
            doc.UploadedByUser?.Id ?? Guid.Empty,
            doc.DateCreated,
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
            link.DateCreated);

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
        var response = new VaultFamilySettingsDto(
            (int)mode,
            mode.ToString(),
            settings?.EmergencyPinHash != null);
        LogApiCall(nameof(GetVaultSettingsAsync), new { currentUserId, familyId }, new { response.EmergencyAccessMode });
        return response;
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var mode = (Domain.Enums.EmergencyAccessMode)request.EmergencyAccessMode;

        string? pinHash = null;
        if (mode == Domain.Enums.EmergencyAccessMode.PinOnly && !string.IsNullOrWhiteSpace(request.EmergencyPin))
        {
            pinHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(request.EmergencyPin)));
        }

        var settingsMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var settings = new Domain.Entities.VaultFamilySettings
        {
            FamilyId             = settingsMember?.FamilyId ?? 0L,
            EmergencyAccessMode  = mode,
            EmergencyPinHash     = pinHash
        };

        await _vaultRepository.UpsertVaultFamilySettingsAsync(settings, cancellationToken);

        var response = new VaultFamilySettingsDto((int)mode, mode.ToString(), pinHash != null);
        LogApiCall(nameof(UpdateVaultSettingsAsync), new { currentUserId, familyId, request.EmergencyAccessMode }, new { response.EmergencyAccessMode });
        return response;
    }

    private async Task<VaultDocument> GetDocumentOrThrowAsync(Guid documentId, Guid familyId, CancellationToken cancellationToken)
    {
        return await _vaultRepository.GetByIdAsync(documentId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode code, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(string fieldName, CancellationToken cancellationToken)
    {
        var message = await GetMessageAsync(FamilyFirstErrorCode.Invalid_MasterData, cancellationToken);
        return new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object request, object response)
    {
        _apiLogService.Log(
            methodName,
            JsonSerializer.Serialize(request),
            JsonSerializer.Serialize(response));
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
