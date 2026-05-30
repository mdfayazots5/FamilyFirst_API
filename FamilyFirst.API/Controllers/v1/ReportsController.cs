using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Reports;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("families/{familyId:guid}/reports/weekly-digest")]
    public async Task<ActionResult<ApiResponse<WeeklyDigestDto>>> GetWeeklyDigest(
        Guid familyId,
        [FromQuery] DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var digest = await _reportService.GetWeeklyDigestAsync(
            GetCurrentUserId(),
            familyId,
            weekStartDate,
            cancellationToken);

        return Ok(ApiResponse<WeeklyDigestDto>.Success(digest));
    }

    [HttpGet("families/{familyId:guid}/children/{childId:guid}/reports/weekly")]
    public async Task<ActionResult<ApiResponse<ChildWeeklyReportDto>>> GetChildWeeklyReport(
        Guid familyId,
        Guid childId,
        [FromQuery] DateOnly? weekStartDate,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetChildWeeklyReportAsync(
            GetCurrentUserId(),
            familyId,
            childId,
            weekStartDate,
            cancellationToken);

        return Ok(ApiResponse<ChildWeeklyReportDto>.Success(report));
    }

    [HttpGet("families/{familyId:guid}/children/{childId:guid}/reports/attendance-summary")]
    public async Task<ActionResult<ApiResponse<AttendanceSummaryDto>>> GetAttendanceSummary(
        Guid familyId,
        Guid childId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var summary = await _reportService.GetAttendanceSummaryAsync(
            GetCurrentUserId(),
            familyId,
            childId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(ApiResponse<AttendanceSummaryDto>.Success(summary));
    }

    // ── Level 2 endpoints ──────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/reports/monthly")]
    public async Task<ActionResult<ApiResponse<MonthlyFamilyReportDto>>> GetMonthlyFamilyReport(
        Guid familyId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetMonthlyFamilyReportAsync(
            GetCurrentUserId(), familyId, year, month, cancellationToken);
        return Ok(ApiResponse<MonthlyFamilyReportDto>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/children/{childId:guid}/reports/monthly")]
    public async Task<ActionResult<ApiResponse<ChildMonthlySummaryDto>>> GetChildMonthlySummary(
        Guid familyId,
        Guid childId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetChildMonthlySummaryAsync(
            GetCurrentUserId(), familyId, childId, year, month, cancellationToken);
        return Ok(ApiResponse<ChildMonthlySummaryDto>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/reports/documents/expiry")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExpiringDocumentItemDto>>>> GetDocumentExpiryReport(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDocumentExpiryReportAsync(
            GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ExpiringDocumentItemDto>>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/reports/health/reminders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HealthReminderItemDto>>>> GetHealthReminderSummary(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetHealthReminderSummaryAsync(
            GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<HealthReminderItemDto>>.Success(result));
    }

    [HttpPost("families/{familyId:guid}/reports/export")]
    public async Task<ActionResult<ApiResponse<ReportExportDto>>> ExportReport(
        Guid familyId,
        ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.ExportReportAsync(
            GetCurrentUserId(), familyId, request, cancellationToken);
        return Created(
            $"/api/v1/families/{familyId}/reports/exports",
            ApiResponse<ReportExportDto>.Success(result, "Report export ready."));
    }

    [HttpGet("families/{familyId:guid}/reports/archive")]
    public async Task<ActionResult<ApiResponse<PaginatedList<ReportArchiveItemDto>>>> GetReportArchive(
        Guid familyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetReportArchiveAsync(
            GetCurrentUserId(), familyId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedList<ReportArchiveItemDto>>.Success(result));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
