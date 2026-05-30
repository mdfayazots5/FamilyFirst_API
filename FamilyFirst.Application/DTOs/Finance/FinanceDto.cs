namespace FamilyFirst.Application.DTOs.Finance;

// ── Dashboard (FF-01) ─────────────────────────────────────────────────────────

public sealed record FinanceDashboardDto(
    FamilyHealthScoreDto HealthScore,
    IReadOnlyCollection<MemberSpendCardDto> MemberCards,
    IReadOnlyCollection<TransactionDto> TodaysTransactions,
    IReadOnlyCollection<FinanceAlertDto> Alerts,
    IReadOnlyCollection<CommitmentDto> UpcomingCommitments);

public sealed record FamilyHealthScoreDto(
    decimal TotalSpendMtd,
    decimal TotalIncomeMtd,
    decimal NetSavingsMtd,
    decimal SavingsRatePct,
    string HealthStatus);           // Green / Amber / Red

public sealed record MemberSpendCardDto(
    Guid MemberId,
    string MemberName,
    string? PhotoUrl,
    int PrivacyTier,
    decimal? TodaySpend,            // null when tier restricts
    decimal? MonthSpend,            // null for Tier 3 (aggregate only)
    decimal? MonthTotal,            // Tier 3: monthly aggregate
    bool IsAboveThreshold);         // Tier 2: any transaction >₹5,000 this month

// ── Transaction (FF-03) ───────────────────────────────────────────────────────

public sealed record TransactionDto(
    Guid TransactionId,
    Guid MemberId,
    string MemberName,
    string? MerchantName,           // null for Tier 2 (hashed); null for Tier 3
    string? MerchantNameHash,       // SHA-256 hash — surfaced only to CFO for Tier 2
    decimal Amount,
    string TransactionType,
    string Category,
    bool IsCategoryBlurred,         // true for Tier 2 blurred categories
    int PrivacyTier,
    string QuestionStatus,
    DateTime ParsedAt);

// ── Question (FF-04) ──────────────────────────────────────────────────────────

public sealed record QuestionTransactionRequest(
    string QuestionType,
    string? ContextNote);

public sealed record TransactionQuestionDto(
    Guid QuestionId,
    Guid TransactionId,
    string QuestionType,
    string? ContextNote,
    DateTime MessageSentAt,
    string? MemberReply,
    DateTime? ReplyReceivedAt,
    string? ResolutionStatus,
    DateTime? ResolvedAt);

// ── Budget (FF-05) ────────────────────────────────────────────────────────────

public sealed record BudgetDto(
    string Category,
    decimal BudgetAmount,
    decimal ActualSpend,
    decimal Remaining,
    decimal? UtilisationPct,
    string Status);                 // Green / Amber / Red

public sealed record SetBudgetRequest(
    string Category,
    decimal BudgetAmount);

// ── Category Breakdown (FF-06) ────────────────────────────────────────────────

public sealed record CategorySpendDto(
    string Category,
    decimal TotalSpend,
    int TransactionCount,
    decimal PctOfTotalSpend,
    string? TopMerchant);

// ── Commitments (FF-09) ───────────────────────────────────────────────────────

public sealed record CommitmentDto(
    Guid CommitmentId,
    Guid MemberId,
    string MemberName,
    string CommitmentName,
    string CommitmentType,
    decimal Amount,
    string FrequencyType,
    DateOnly NextDueDate,
    string Status,
    bool IsConfirmed);

// ── Finance Alerts ────────────────────────────────────────────────────────────

public sealed record FinanceAlertDto(
    string AlertType,
    string Message,
    string Severity,               // Info / Warning / Critical
    Guid? RelatedTransactionId,
    Guid? RelatedCommitmentId);

// ── Settings (FF-08) ─────────────────────────────────────────────────────────

public sealed record FinanceSettingsDto(
    Guid? CfoMemberId,
    string? CfoMemberName,
    bool IsModuleEnabled,
    IReadOnlyCollection<MemberFinanceSettingDto> MemberSettings);

public sealed record MemberFinanceSettingDto(
    Guid MemberId,
    string MemberName,
    int PrivacyTier,
    string ConsentStatus,
    DateTime? ConsentGivenAt,
    DateTime? OptedOutAt);

public sealed record UpdateFinanceSettingsRequest(
    Guid? CfoMemberId,
    IReadOnlyCollection<MemberTierChangeDto>? MemberTierChanges);

public sealed record MemberTierChangeDto(
    Guid MemberId,
    int PrivacyTier);

// ── Consent (FF-07) ──────────────────────────────────────────────────────────

public sealed record InviteConsentRequest(
    Guid MemberId,
    int PrivacyTier);

public sealed record AcceptFinanceConsentRequest(
    string ConsentToken,
    string ConsentVersion);

public sealed record ConsentInviteDto(
    Guid ConsentId,
    string MemberName,
    string CfoName,
    int PrivacyTier,
    string ConsentVersion,
    string PrivacyTierDescription);
