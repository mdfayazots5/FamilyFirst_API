using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Reports;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IReportService
{
    Task<WeeklyDigestDto> GetWeeklyDigestAsync(
        Guid currentUserId,
        Guid familyId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken);

    Task<WeeklyDigestDto> GenerateWeeklyDigestAsync(
        Guid familyId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken);

    Task<ChildWeeklyReportDto> GetChildWeeklyReportAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken);

    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);

    // ── Level 2 extensions ────────────────────────────────────────────────────

    Task<MonthlyFamilyReportDto> GetMonthlyFamilyReportAsync(
        Guid currentUserId,
        Guid familyId,
        int? year,
        int? month,
        CancellationToken cancellationToken);

    Task<ChildMonthlySummaryDto> GetChildMonthlySummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        int? year,
        int? month,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExpiringDocumentItemDto>> GetDocumentExpiryReportAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HealthReminderItemDto>> GetHealthReminderSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<ReportExportDto> ExportReportAsync(
        Guid currentUserId,
        Guid familyId,
        ExportReportRequest request,
        CancellationToken cancellationToken);

    Task<PaginatedList<ReportArchiveItemDto>> GetReportArchiveAsync(
        Guid currentUserId,
        Guid familyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
