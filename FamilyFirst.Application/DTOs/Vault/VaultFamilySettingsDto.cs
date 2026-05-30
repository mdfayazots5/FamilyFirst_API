namespace FamilyFirst.Application.DTOs.Vault;

public sealed record VaultFamilySettingsDto(
    int EmergencyAccessMode,
    string EmergencyAccessModeLabel,
    bool HasEmergencyPin
);

public sealed record UpdateVaultFamilySettingsRequest(
    int EmergencyAccessMode,
    string? EmergencyPin
);
