using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/families/{familyId:guid}/admin")]
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
            $"/api/v1/families/{familyId}/admin/attendance-statuses/{item.StatusId}",
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

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
