namespace FamilyFirst.Application.DTOs.Vault;

public sealed record DocumentDto(
    Guid DocumentId,
    string DocumentName,
    int Category,
    string CategoryName,
    Guid MemberId,
    string MemberName,
    Guid UploadedByUserId,
    DateTime UploadDate,
    DateTime? ExpiryDate,
    string ExpiryStatus,
    string? ThumbnailUrl,
    string[] Tags,
    bool IsEmergencyPriority,
    int VersionNumber
);
