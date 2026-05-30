namespace FamilyFirst.Application.DTOs.Admin;

// ── AC-04 extended — Alert Thresholds ─────────────────────────────────────────

public sealed record AlertThresholdsDto(
    // Finance
    decimal FinanceLargeTransactionThreshold,   // Default: 5000 (Rs.) — surfaces to CFO regardless of tier
    // Document expiry lead times (days before expiry to start reminders)
    int DocumentExpiryLeadDaysDefault,          // Default: 30 — used when no per-category override exists
    int DocumentExpiryLeadDaysIdentity,         // Default: 60 (ID docs need more lead time)
    int DocumentExpiryLeadDaysMedical,          // Default: 30
    int DocumentExpiryLeadDaysInsurance,        // Default: 45
    // Location / Safety
    int LateArrivalToleranceMinutes,            // Default: 0 — alert fires at exact LateAlertTime
    int LocationStaleThresholdMinutes);         // Default: 60 — pin shown as stale after this

public sealed record UpdateAlertThresholdsRequest(
    decimal? FinanceLargeTransactionThreshold,
    int? DocumentExpiryLeadDaysDefault,
    int? DocumentExpiryLeadDaysIdentity,
    int? DocumentExpiryLeadDaysMedical,
    int? DocumentExpiryLeadDaysInsurance,
    int? LateArrivalToleranceMinutes,
    int? LocationStaleThresholdMinutes);
