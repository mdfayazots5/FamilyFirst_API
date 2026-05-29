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

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
