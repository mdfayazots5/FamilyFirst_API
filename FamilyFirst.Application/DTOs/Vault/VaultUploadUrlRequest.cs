namespace FamilyFirst.Application.DTOs.Vault;

public sealed record VaultUploadUrlRequest(
    string FileName,
    string ContentType,
    int Category
);
