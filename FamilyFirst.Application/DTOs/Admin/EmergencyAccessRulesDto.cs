namespace FamilyFirst.Application.DTOs.Admin;

// ── DV-07 / Emergency Access Configuration ────────────────────────────────────

public sealed record EmergencyAccessRulesDto(
    // Emergency folder & card access mode: LoginRequired / PinOnly / NoLogin
    string AccessMode,
    // How long shared emergency links stay valid (hours). Max 168 (7 days). Default 72.
    int EmergencyLinkExpiryHours,
    // Emergency contacts who receive SOS + emergency alerts (max 3)
    IReadOnlyCollection<EmergencyContactDto> EmergencyContacts);

public sealed record EmergencyContactDto(
    string Name,
    string PhoneNumber,
    string Relationship);

public sealed record UpdateEmergencyAccessRulesRequest(
    string? AccessMode,
    int? EmergencyLinkExpiryHours,
    IReadOnlyCollection<EmergencyContactDto>? EmergencyContacts);

// ── AC-06 Finance Privacy Configuration ───────────────────────────────────────

public sealed record FinancePrivacyConfigDto(
    // Default tier assigned to new adult earning members during consent invite
    int DefaultAdultEarningMemberTier,         // Default: 2 (CategoryOnly)
    // Default tier for financially independent members
    int DefaultIndependentMemberTier,          // Default: 3 (AggregateOnly)
    // How often monthly consent reminder SMS is sent (days)
    int ConsentReminderIntervalDays,           // Default: 30
    // Whether salary credits are auto-excluded from expense calculations
    bool AutoExcludeSalaryCredits);

public sealed record UpdateFinancePrivacyConfigRequest(
    int? DefaultAdultEarningMemberTier,
    int? DefaultIndependentMemberTier,
    int? ConsentReminderIntervalDays,
    bool? AutoExcludeSalaryCredits);
