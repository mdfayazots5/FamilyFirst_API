using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IFamilyAdminService _familyAdminService;

    public AttendanceController(
        IAttendanceService attendanceService,
        IFamilyAdminService familyAdminService)
    {
        _attendanceService = attendanceService;
        _familyAdminService = familyAdminService;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ApiResponse<AttendanceSessionDto>>> CreateSession(
        Guid familyId,
        CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _attendanceService.CreateSessionAsync(GetCurrentUserId(), familyId, request, cancellationToken);

        return Created(
            $"/api/families/{familyId}/attendance/sessions/{session.SessionId}",
            ApiResponse<AttendanceSessionDto>.Success(session, "Attendance session created."));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceSessionDto>>>> ListSessions(
        Guid familyId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var sessions = await _attendanceService.ListSessionsAsync(GetCurrentUserId(), familyId, date, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<AttendanceSessionDto>>.Success(sessions));
    }

    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<AttendanceSessionDto>>> GetSession(
        Guid familyId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _attendanceService.GetSessionAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            sessionId,
            cancellationToken);

        return Ok(ApiResponse<AttendanceSessionDto>.Success(session));
    }

    [HttpPost("sessions/{sessionId:guid}/submit")]
    public async Task<ActionResult<ApiResponse<AttendanceSessionDto>>> SubmitAttendance(
        Guid familyId,
        Guid sessionId,
        SubmitAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _attendanceService.SubmitAttendanceAsync(
            GetCurrentUserId(),
            familyId,
            sessionId,
            request,
            cancellationToken);

        return Ok(ApiResponse<AttendanceSessionDto>.Success(session, "Attendance submitted."));
    }

    [HttpPut("sessions/{sessionId:guid}/records/{recordId:guid}")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordDto>>> EditAttendanceRecord(
        Guid familyId,
        Guid sessionId,
        Guid recordId,
        EditAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceService.EditAttendanceRecordAsync(
            GetCurrentUserId(),
            familyId,
            sessionId,
            recordId,
            request,
            cancellationToken);

        return Ok(ApiResponse<AttendanceRecordDto>.Success(record, "Attendance record updated."));
    }

    [HttpGet("~/api/families/{familyId:guid}/children/{childId:guid}/attendance")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>>> ListChildAttendance(
        Guid familyId,
        Guid childId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var records = await _attendanceService.ListChildAttendanceAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>.Success(records));
    }

    [HttpGet("sessions/{sessionId:guid}/records")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>>> ListSessionRecords(
        Guid familyId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var records = await _attendanceService.ListSessionRecordsAsync(
            GetCurrentUserId(),
            familyId,
            sessionId,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>.Success(records));
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustomAttendanceStatusDto>>>> ListAttendanceStatuses(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var statuses = await _familyAdminService.GetAttendanceStatusesAsync(
            GetCurrentUserId(),
            familyId,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<CustomAttendanceStatusDto>>.Success(statuses));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }

    private Guid? GetCurrentChildProfileId()
    {
        var childProfileId = User.FindFirstValue("childProfileId");

        return Guid.TryParse(childProfileId, out var parsedChildProfileId) ? parsedChildProfileId : null;
    }
}
