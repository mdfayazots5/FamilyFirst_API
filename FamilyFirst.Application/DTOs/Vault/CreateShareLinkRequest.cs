namespace FamilyFirst.Application.DTOs.Vault;

public sealed record CreateShareLinkRequest(
    int? ExpiryHours,
    bool? AllowDownload
);

public sealed record ShareLinkDto(
    Guid ShareLinkId,
    string ShareUrl,
    DateTime ExpiresAt,
    bool AllowDownload,
    bool IsRevoked,
    DateTime? LastAccessedAt,
    DateTime CreatedAt
);
