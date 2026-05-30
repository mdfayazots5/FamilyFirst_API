namespace FamilyFirst.Application.DTOs.Vault;

public sealed record VaultUploadUrlDto(
    string UploadUrl,
    string FileUrl,
    DateTime ExpiresAtUtc
);
