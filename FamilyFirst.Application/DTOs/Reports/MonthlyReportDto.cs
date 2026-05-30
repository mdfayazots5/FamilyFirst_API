namespace FamilyFirst.Application.DTOs.Reports;

// ── Monthly Family Report (RP-03) ─────────────────────────────────────────────

public sealed record MonthlyFamilyReportDto(
    Guid FamilyId,
    string FamilyName,
    int Year,
    int Month,
    IReadOnlyCollection<MonthlyChildSummaryItemDto> Children,
    int TotalFeedbackCount,
    decimal FeedbackResolutionRate,
    IReadOnlyCollection<ExpiringDocumentItemDto> ExpiringDocuments,
    IReadOnlyCollection<HealthReminderItemDto> HealthReminders,
    MonthlyFinanceSnapshotDto? FinanceSnapshot,
    DateTime GeneratedAt,
    string NarrativeHeadline);

public sealed record MonthlyChildSummaryItemDto(
    Guid ChildProfileId,
    string ChildName,
    decimal AttendanceRate,
    decimal AttendanceDelta,
    decimal TaskRate,
    decimal TaskDelta,
    int FeedbackCount,
    int CoinsEarned,
    int CoinsSpent);

// ── Child Monthly Summary (RP-04) ─────────────────────────────────────────────

public sealed record ChildMonthlySummaryDto(
    Guid ChildProfileId,
    string ChildName,
    int Year,
    int Month,
    decimal AttendanceRate,
    int AttendanceSessions,
    int AttendancePresentCount,
    int AttendanceAbsentCount,
    decimal TaskRate,
    int TaskAssignedCount,
    int TaskApprovedCount,
    int FeedbackCount,
    IReadOnlyDictionary<string, int> FeedbackByType,
    int CoinsEarned,
    int CoinsSpent,
    IReadOnlyCollection<PillarScoreSnapshotDto> PillarScores,
    string NarrativeSummary);

public sealed record PillarScoreSnapshotDto(
    DateOnly Month,
    int StudyScore,
    int CleanlinessScore,
    int DisciplineScore,
    int ScreenControlScore,
    int ResponsibilityScore);

// ── Document Expiry Report (RP-06) ────────────────────────────────────────────

public sealed record ExpiringDocumentItemDto(
    Guid DocumentId,
    string DocumentName,
    string Category,
    DateOnly ExpiryDate,
    int DaysUntilExpiry);

// ── Health Reminder Summary (RP-07) ───────────────────────────────────────────

public sealed record HealthReminderItemDto(
    Guid MemberId,
    string MemberName,
    string ReminderType,
    string Description,
    DateOnly? DueDate);

// ── Finance Snapshot (embedded in monthly family report) ──────────────────────

public sealed record MonthlyFinanceSnapshotDto(
    decimal TotalIncome,
    decimal TotalSpend,
    decimal SavingsRate,
    string? TopCategory,
    int AlertCount);

// ── Export (RP-08) ────────────────────────────────────────────────────────────

public sealed record ReportExportDto(
    string DownloadUrl,
    DateTime ExpiresAtUtc,
    string Format);
