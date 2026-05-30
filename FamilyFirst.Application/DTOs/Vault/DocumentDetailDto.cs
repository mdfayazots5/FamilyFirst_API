namespace FamilyFirst.Application.DTOs.Vault;

public sealed record DocumentDetailDto(
    Guid DocumentId,
    string DocumentName,
    int Category,
    string CategoryName,
    int Visibility,
    Guid MemberId,
    string MemberName,
    Guid UploadedByUserId,
    DateTime UploadDate,
    DateTime? ExpiryDate,
    string ExpiryStatus,
    string FileUrl,
    string? ThumbnailUrl,
    string[] Tags,
    bool IsEmergencyPriority,
    int VersionNumber,
    IReadOnlyCollection<DocumentVersionDto> VersionHistory,
    IReadOnlyCollection<ShareLinkDto> ActiveShareLinks
);

public sealed record DocumentVersionDto(
    Guid VersionId,
    int VersionNumber,
    string FileUrl,
    Guid UploadedByUserId,
    DateTime ArchivedAt
);
