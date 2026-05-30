using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultFamilySettings : BaseEntity
{
    public Guid FamilyId { get; set; }

    public EmergencyAccessMode EmergencyAccessMode { get; set; } = EmergencyAccessMode.LoginRequired;

    public string? EmergencyPinHash { get; set; }

    public Family Family { get; set; } = null!;

    // ── Level 2 Admin Config (script 066) ─────────────────────────────────────

    // Storage (AC-01 / AC-02)
    public string StorageMode { get; set; } = "AppManaged";
    public int StorageQuotaAlertThresholdPct { get; set; } = 90;
    public int OfflineCacheSizeMb { get; set; } = 500;
    public string? HybridRoutingJson { get; set; }

    // Emergency (DV-07)
    public int EmergencyLinkExpiryHours { get; set; } = 72;
    public string? EmergencyContactsJson { get; set; }

    // Alert Thresholds (AC-04)
    public decimal FinanceLargeTransactionThreshold { get; set; } = 5000m;
    public int DocExpiryLeadDaysDefault { get; set; } = 30;
    public int DocExpiryLeadDaysIdentity { get; set; } = 60;
    public int DocExpiryLeadDaysMedical { get; set; } = 30;
    public int DocExpiryLeadDaysInsurance { get; set; } = 45;
    public int LateArrivalToleranceMinutes { get; set; }
    public int LocationStaleThresholdMinutes { get; set; } = 60;

    // Finance Privacy (AC-06)
    public int DefaultAdultEarningMemberTier { get; set; } = 2;
    public int DefaultIndependentMemberTier { get; set; } = 3;
    public int ConsentReminderIntervalDays { get; set; } = 30;
    public bool AutoExcludeSalaryCredits { get; set; } = true;
}
