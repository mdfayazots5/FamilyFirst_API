using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
// StorageConfigDto, AlertThresholdsDto, EmergencyAccessRulesDto, FinancePrivacyConfigDto — same namespace
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/admin")]
public sealed class FamilyAdminController : ControllerBase
{
    private readonly IFamilyAdminService _familyAdminService;

    public FamilyAdminController(IFamilyAdminService familyAdminService)
    {
        _familyAdminService = familyAdminService;
    }

    [HttpGet("panel")]
    public async Task<ActionResult<ApiResponse<FamilyAdminPanelDto>>> GetPanel(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var panel = await _familyAdminService.GetPanelAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<FamilyAdminPanelDto>.Success(panel));
    }

    [HttpGet("module-visibility")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ModuleVisibilityDto>>>> GetModuleVisibility(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var items = await _familyAdminService.GetModuleVisibilityAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ModuleVisibilityDto>>.Success(items));
    }

    [HttpPut("module-visibility")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ModuleVisibilityDto>>>> UpdateModuleVisibility(
        Guid familyId,
        UpdateModuleVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var items = await _familyAdminService.UpdateModuleVisibilityAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ModuleVisibilityDto>>.Success(items, "Module visibility updated."));
    }

    [HttpGet("notification-rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationRuleDto>>>> GetNotificationRules(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var rules = await _familyAdminService.GetNotificationRulesAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<NotificationRuleDto>>.Success(rules));
    }

    [HttpPut("notification-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationRuleDto>>> UpdateNotificationRule(
        Guid familyId,
        Guid ruleId,
        UpdateNotificationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await _familyAdminService.UpdateNotificationRuleAsync(
            GetCurrentUserId(),
            familyId,
            ruleId,
            request,
            cancellationToken);
        return Ok(ApiResponse<NotificationRuleDto>.Success(rule, "Notification rule updated."));
    }

    [HttpGet("attendance-statuses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustomAttendanceStatusDto>>>> GetAttendanceStatuses(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var items = await _familyAdminService.GetAttendanceStatusesAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CustomAttendanceStatusDto>>.Success(items));
    }

    [HttpPost("attendance-statuses")]
    public async Task<ActionResult<ApiResponse<CustomAttendanceStatusDto>>> CreateAttendanceStatus(
        Guid familyId,
        CreateCustomAttendanceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _familyAdminService.CreateAttendanceStatusAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);
        return Created(
            $"/api/families/{familyId}/admin/attendance-statuses/{item.StatusId}",
            ApiResponse<CustomAttendanceStatusDto>.Success(item, "Attendance status created."));
    }

    [HttpDelete("attendance-statuses/{statusId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAttendanceStatus(
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken)
    {
        var deleted = await _familyAdminService.DeleteAttendanceStatusAsync(
            GetCurrentUserId(),
            familyId,
            statusId,
            cancellationToken);
        return Ok(ApiResponse<bool>.Success(deleted, "Attendance status deleted."));
    }

    // ── Level 2 Admin Config ───────────────────────────────────────────────────

    [HttpGet("storage")]
    public async Task<ActionResult<ApiResponse<StorageConfigDto>>> GetStorageConfig(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.GetStorageConfigAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<StorageConfigDto>.Success(result));
    }

    [HttpPut("storage")]
    public async Task<ActionResult<ApiResponse<StorageConfigDto>>> UpdateStorageConfig(
        Guid familyId,
        UpdateStorageConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.UpdateStorageConfigAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<StorageConfigDto>.Success(result, "Storage configuration updated."));
    }

    [HttpGet("alert-thresholds")]
    public async Task<ActionResult<ApiResponse<AlertThresholdsDto>>> GetAlertThresholds(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.GetAlertThresholdsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<AlertThresholdsDto>.Success(result));
    }

    [HttpPut("alert-thresholds")]
    public async Task<ActionResult<ApiResponse<AlertThresholdsDto>>> UpdateAlertThresholds(
        Guid familyId,
        UpdateAlertThresholdsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.UpdateAlertThresholdsAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<AlertThresholdsDto>.Success(result, "Alert thresholds updated."));
    }

    [HttpGet("emergency-config")]
    public async Task<ActionResult<ApiResponse<EmergencyAccessRulesDto>>> GetEmergencyConfig(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.GetEmergencyAccessRulesAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<EmergencyAccessRulesDto>.Success(result));
    }

    [HttpPut("emergency-config")]
    public async Task<ActionResult<ApiResponse<EmergencyAccessRulesDto>>> UpdateEmergencyConfig(
        Guid familyId,
        UpdateEmergencyAccessRulesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.UpdateEmergencyAccessRulesAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<EmergencyAccessRulesDto>.Success(result, "Emergency access configuration updated."));
    }

    [HttpGet("finance-config")]
    public async Task<ActionResult<ApiResponse<FinancePrivacyConfigDto>>> GetFinancePrivacyConfig(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.GetFinancePrivacyConfigAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<FinancePrivacyConfigDto>.Success(result));
    }

    [HttpPut("finance-config")]
    public async Task<ActionResult<ApiResponse<FinancePrivacyConfigDto>>> UpdateFinancePrivacyConfig(
        Guid familyId,
        UpdateFinancePrivacyConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _familyAdminService.UpdateFinancePrivacyConfigAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<FinancePrivacyConfigDto>.Success(result, "Finance privacy configuration updated."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
