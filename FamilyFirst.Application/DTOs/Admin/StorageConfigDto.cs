namespace FamilyFirst.Application.DTOs.Admin;

// ── AC-01 / AC-02 Storage Provider Configuration ──────────────────────────────

public sealed record StorageConfigDto(
    string StorageMode,                          // AppManaged / GoogleDrive / Hybrid
    bool GoogleDriveConnected,
    string? GoogleDriveEmail,
    string? GoogleDriveFolderName,
    int StorageQuotaAlertThresholdPct,           // 75 / 90 / 95
    int OfflineCacheSizeMb,                      // 500 / 1000 / 2000
    long StorageUsedBytes,
    long StorageQuotaBytes,
    IReadOnlyCollection<HybridRoutingRuleDto> HybridRouting);

public sealed record HybridRoutingRuleDto(
    string Category,
    string Provider);                            // App / GoogleDrive

public sealed record UpdateStorageConfigRequest(
    string StorageMode,
    int? StorageQuotaAlertThresholdPct,
    int? OfflineCacheSizeMb,
    IReadOnlyCollection<HybridRoutingRuleDto>? HybridRouting);
