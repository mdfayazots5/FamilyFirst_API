using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Calendar;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("families/{familyId:guid}/calendar/events")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventDto>>>> ListEvents(
        Guid familyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var calendarEvents = await _calendarService.ListEventsAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<EventDto>>.Success(calendarEvents));
    }

    [HttpPost("families/{familyId:guid}/calendar/events")]
    public async Task<ActionResult<ApiResponse<EventDto>>> CreateEvent(
        Guid familyId,
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await _calendarService.CreateEventAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);

        return Created(
            $"/api/v1/families/{familyId}/calendar/events/{calendarEvent.EventId}",
            ApiResponse<EventDto>.Success(calendarEvent, "Calendar event created."));
    }

    [HttpGet("families/{familyId:guid}/calendar/events/{eventId:guid}")]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetEvent(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await _calendarService.GetEventAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            eventId,
            cancellationToken);

        return Ok(ApiResponse<EventDto>.Success(calendarEvent));
    }

    [HttpPut("families/{familyId:guid}/calendar/events/{eventId:guid}")]
    public async Task<ActionResult<ApiResponse<EventDto>>> UpdateEvent(
        Guid familyId,
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await _calendarService.UpdateEventAsync(
            GetCurrentUserId(),
            familyId,
            eventId,
            request,
            cancellationToken);

        return Ok(ApiResponse<EventDto>.Success(calendarEvent, "Calendar event updated."));
    }

    [HttpDelete("families/{familyId:guid}/calendar/events/{eventId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEvent(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var deleted = await _calendarService.DeleteEventAsync(
            GetCurrentUserId(),
            familyId,
            eventId,
            cancellationToken);

        return Ok(ApiResponse<bool>.Success(deleted, "Calendar event deleted."));
    }

    [HttpGet("families/{familyId:guid}/calendar/upcoming")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventDto>>>> ListUpcomingEvents(
        Guid familyId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var calendarEvents = await _calendarService.ListUpcomingEventsAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            days,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<EventDto>>.Success(calendarEvents));
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
