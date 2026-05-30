namespace FamilyFirst.Application.DTOs.Vault;

public sealed record UpdateVaultDocumentRequest(
    string? DocumentName,
    DateTime? ExpiryDate,
    string[]? Tags,
    int? Visibility,
    bool? IsEmergencyPriority,
    string? NewFileUrl
);
