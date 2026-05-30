namespace FamilyFirst.Application.DTOs.Vault;

public sealed record CreateVaultDocumentRequest(
    string DocumentName,
    Guid MemberId,
    int Category,
    string FileUrl,
    DateTime? ExpiryDate,
    string[]? Tags,
    int? Visibility,
    bool IsEmergencyPriority
);
