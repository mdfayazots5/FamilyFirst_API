using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Medical;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Route("api")]
public sealed class MedicalController : ControllerBase
{
    private readonly IMedicalService _medicalService;

    public MedicalController(IMedicalService medicalService)
    {
        _medicalService = medicalService;
    }

    // ── Health Profiles ────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HealthProfileSummaryDto>>>> ListHealthProfiles(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.ListHealthProfilesAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<HealthProfileSummaryDto>>.Success(result));
    }

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<HealthProfileDto>>> GetHealthProfile(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.GetHealthProfileAsync(GetCurrentUserId(), familyId, memberId, cancellationToken);
        return Ok(ApiResponse<HealthProfileDto>.Success(result));
    }

    [Authorize]
    [HttpPut("families/{familyId:guid}/health-profiles/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<HealthProfileDto>>> UpdateHealthProfile(
        Guid familyId,
        Guid memberId,
        UpdateHealthProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.UpdateHealthProfileAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Ok(ApiResponse<HealthProfileDto>.Success(result, "Health profile updated."));
    }

    // ── Prescriptions ──────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("families/{familyId:guid}/health-profiles/{memberId:guid}/prescriptions")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> AddPrescription(
        Guid familyId,
        Guid memberId,
        AddPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.AddPrescriptionAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Created(
            $"/api/families/{familyId}/health-profiles/{memberId}/prescriptions/{result.PrescriptionId}",
            ApiResponse<PrescriptionDto>.Success(result, "Prescription added."));
    }

    [Authorize]
    [HttpDelete("families/{familyId:guid}/health-profiles/{memberId:guid}/prescriptions/{prescriptionId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePrescription(
        Guid familyId,
        Guid memberId,
        Guid prescriptionId,
        CancellationToken cancellationToken)
    {
        await _medicalService.DeletePrescriptionAsync(
            GetCurrentUserId(), familyId, memberId, prescriptionId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Prescription deleted."));
    }

    // ── Vaccinations ───────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles/{memberId:guid}/vaccinations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<VaccinationDto>>>> ListVaccinations(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.ListVaccinationsAsync(
            GetCurrentUserId(), familyId, memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<VaccinationDto>>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/health-profiles/{memberId:guid}/vaccinations")]
    public async Task<ActionResult<ApiResponse<VaccinationDto>>> AddVaccination(
        Guid familyId,
        Guid memberId,
        AddVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.AddVaccinationAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Created(
            $"/api/families/{familyId}/health-profiles/{memberId}/vaccinations/{result.VaccinationId}",
            ApiResponse<VaccinationDto>.Success(result, "Vaccination added."));
    }

    [Authorize]
    [HttpPut("families/{familyId:guid}/health-profiles/{memberId:guid}/vaccinations/{vaccinationId:guid}/status")]
    public async Task<ActionResult<ApiResponse<VaccinationDto>>> UpdateVaccinationStatus(
        Guid familyId,
        Guid memberId,
        Guid vaccinationId,
        UpdateVaccinationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.UpdateVaccinationStatusAsync(
            GetCurrentUserId(), familyId, memberId, vaccinationId, request, cancellationToken);
        return Ok(ApiResponse<VaccinationDto>.Success(result, "Vaccination status updated."));
    }

    // ── Health Timeline ────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles/{memberId:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<PaginatedList<HealthRecordDto>>>> ListTimeline(
        Guid familyId,
        Guid memberId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _medicalService.ListTimelineAsync(
            GetCurrentUserId(), familyId, memberId,
            eventType, fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedList<HealthRecordDto>>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/health-profiles/{memberId:guid}/records")]
    public async Task<ActionResult<ApiResponse<HealthRecordDto>>> AddHealthRecord(
        Guid familyId,
        Guid memberId,
        AddHealthRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.AddHealthRecordAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Created(
            $"/api/families/{familyId}/health-profiles/{memberId}/records/{result.HealthRecordId}",
            ApiResponse<HealthRecordDto>.Success(result, "Health record added."));
    }

    // ── Emergency Card ─────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles/{memberId:guid}/emergency-card")]
    public async Task<ActionResult<ApiResponse<EmergencyCardDto>>> GetEmergencyCard(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.GetEmergencyCardAsync(familyId, memberId, cancellationToken);
        return Ok(ApiResponse<EmergencyCardDto>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/health-profiles/{memberId:guid}/emergency-card/share")]
    public async Task<ActionResult<ApiResponse<EmergencyCardShareDto>>> ShareEmergencyCard(
        Guid familyId,
        Guid memberId,
        ShareEmergencyCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.ShareEmergencyCardAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Created(result.ShareLink, ApiResponse<EmergencyCardShareDto>.Success(result, "Emergency card link created."));
    }

    [HttpGet("medical/emergency-card/{token}")]
    public async Task<ActionResult<ApiResponse<EmergencyCardDto>>> GetEmergencyCardByToken(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.GetEmergencyCardByTokenAsync(token, cancellationToken);
        return Ok(ApiResponse<EmergencyCardDto>.Success(result));
    }

    // ── Height / Weight ────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("families/{familyId:guid}/health-profiles/{memberId:guid}/height-weight")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HeightWeightDto>>>> ListHeightWeight(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.ListHeightWeightAsync(
            GetCurrentUserId(), familyId, memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<HeightWeightDto>>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/health-profiles/{memberId:guid}/height-weight")]
    public async Task<ActionResult<ApiResponse<HeightWeightDto>>> AddHeightWeight(
        Guid familyId,
        Guid memberId,
        AddHeightWeightRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _medicalService.AddHeightWeightAsync(
            GetCurrentUserId(), familyId, memberId, request, cancellationToken);
        return Created(
            $"/api/families/{familyId}/health-profiles/{memberId}/height-weight/{result.HeightWeightRecordId}",
            ApiResponse<HeightWeightDto>.Success(result, "Height/weight record added."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
