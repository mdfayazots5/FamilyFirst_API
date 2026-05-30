using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Safety;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class SafetyController : ControllerBase
{
    private readonly ISafetyService _safetyService;

    public SafetyController(ISafetyService safetyService)
    {
        _safetyService = safetyService;
    }

    // ── Map View ───────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/safety/map")]
    public async Task<ActionResult<ApiResponse<MapViewDto>>> GetMapView(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _safetyService.GetMapViewAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<MapViewDto>.Success(result));
    }

    // ── Location Update (device posting own location) ──────────────────────

    [HttpPost("families/{familyId:guid}/safety/location")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateLocation(
        Guid familyId,
        UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        await _safetyService.UpdateLocationAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    // ── Safe Zones ─────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/safety/zones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SafeZoneDto>>>> ListZones(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _safetyService.ListZonesAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SafeZoneDto>>.Success(result));
    }

    [HttpPost("families/{familyId:guid}/safety/zones")]
    public async Task<ActionResult<ApiResponse<SafeZoneDto>>> CreateZone(
        Guid familyId,
        CreateSafeZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _safetyService.CreateZoneAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Created(
            $"/api/v1/families/{familyId}/safety/zones/{result.ZoneId}",
            ApiResponse<SafeZoneDto>.Success(result, "Safe zone created."));
    }

    [HttpPut("families/{familyId:guid}/safety/zones/{zoneId:guid}")]
    public async Task<ActionResult<ApiResponse<SafeZoneDto>>> UpdateZone(
        Guid familyId,
        Guid zoneId,
        UpdateSafeZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _safetyService.UpdateZoneAsync(GetCurrentUserId(), familyId, zoneId, request, cancellationToken);
        return Ok(ApiResponse<SafeZoneDto>.Success(result, "Safe zone updated."));
    }

    [HttpDelete("families/{familyId:guid}/safety/zones/{zoneId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteZone(
        Guid familyId,
        Guid zoneId,
        CancellationToken cancellationToken)
    {
        await _safetyService.DeleteZoneAsync(GetCurrentUserId(), familyId, zoneId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Safe zone deleted."));
    }

    // ── Location Alerts ────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/safety/alerts")]
    public async Task<ActionResult<ApiResponse<PaginatedList<LocationAlertDto>>>> ListAlerts(
        Guid familyId,
        [FromQuery] Guid? memberId,
        [FromQuery] string? alertType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _safetyService.ListAlertsAsync(
            GetCurrentUserId(), familyId, memberId, alertType,
            fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedList<LocationAlertDto>>.Success(result));
    }

    [HttpPut("families/{familyId:guid}/safety/alerts/{alertId:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<LocationAlertDto>>> ResolveAlert(
        Guid familyId,
        Guid alertId,
        ResolveAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _safetyService.ResolveAlertAsync(
            GetCurrentUserId(), familyId, alertId, request, cancellationToken);
        return Ok(ApiResponse<LocationAlertDto>.Success(result, "Alert resolved."));
    }

    // ── SOS ────────────────────────────────────────────────────────────────

    [HttpPost("families/{familyId:guid}/safety/sos")]
    public async Task<ActionResult<ApiResponse<SosEventDto>>> TriggerSos(
        Guid familyId,
        TriggerSosRequest request,
        CancellationToken cancellationToken)
    {
        var childProfileId = GetCurrentChildProfileId();
        if (!childProfileId.HasValue)
            return Forbid();

        var result = await _safetyService.TriggerSosAsync(
            GetCurrentUserId(), familyId, childProfileId.Value, request, cancellationToken);

        return Created(
            $"/api/v1/families/{familyId}/safety/alerts",
            ApiResponse<SosEventDto>.Success(result, "SOS dispatched. Parents have been notified."));
    }

    // ── Location Settings ──────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/safety/settings")]
    public async Task<ActionResult<ApiResponse<LocationSettingsDto>>> GetSettings(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _safetyService.GetSettingsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<LocationSettingsDto>.Success(result));
    }

    [HttpPut("families/{familyId:guid}/safety/settings")]
    public async Task<ActionResult<ApiResponse<LocationSettingsDto>>> UpdateSettings(
        Guid familyId,
        UpdateLocationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _safetyService.UpdateSettingsAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<LocationSettingsDto>.Success(result, "Location settings updated."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }

    private Guid? GetCurrentChildProfileId()
    {
        var raw = User.FindFirstValue("childProfileId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
