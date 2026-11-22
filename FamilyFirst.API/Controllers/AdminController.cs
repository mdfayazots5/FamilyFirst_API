using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize(Policy = "SuperAdmin")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _adminService.GetDashboardAsync(GetCurrentUserRole(), cancellationToken);
        return Ok(ApiResponse<AdminDashboardDto>.Success(dashboard));
    }

    [HttpGet("families")]
    public async Task<ActionResult<ApiResponse<PaginatedList<AdminFamilySummaryDto>>>> SearchFamilies(
        [FromQuery] AdminFamilySearchRequest request,
        CancellationToken cancellationToken)
    {
        var families = await _adminService.SearchFamiliesAsync(GetCurrentUserRole(), request, cancellationToken);
        return Ok(ApiResponse<PaginatedList<AdminFamilySummaryDto>>.Success(families));
    }

    [HttpGet("families/{familyId:guid}")]
    public async Task<ActionResult<ApiResponse<AdminFamilyDetailDto>>> GetFamilyDetail(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var family = await _adminService.GetFamilyDetailAsync(GetCurrentUserRole(), familyId, cancellationToken);
        return Ok(ApiResponse<AdminFamilyDetailDto>.Success(family));
    }

    [HttpDelete("families/{familyId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockFamily(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var blocked = await _adminService.BlockFamilyAsync(GetCurrentUserRole(), familyId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(blocked, "Family blocked."));
    }

    [HttpGet("analytics/overview")]
    public async Task<ActionResult<ApiResponse<AnalyticsOverviewDto>>> GetAnalyticsOverview(
        CancellationToken cancellationToken)
    {
        var analytics = await _adminService.GetAnalyticsOverviewAsync(GetCurrentUserRole(), cancellationToken);
        return Ok(ApiResponse<AnalyticsOverviewDto>.Success(analytics));
    }

    [HttpGet("feature-flags")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FeatureFlagDto>>>> ListFeatureFlags(
        CancellationToken cancellationToken)
    {
        var featureFlags = await _adminService.ListFeatureFlagsAsync(GetCurrentUserRole(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<FeatureFlagDto>>.Success(featureFlags));
    }

    [HttpPut("feature-flags/{flag}")]
    public async Task<ActionResult<ApiResponse<FeatureFlagDto>>> UpdateFeatureFlag(
        string flag,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        var featureFlag = await _adminService.UpdateFeatureFlagAsync(
            GetCurrentUserRole(),
            flag,
            request,
            cancellationToken);

        return Ok(ApiResponse<FeatureFlagDto>.Success(featureFlag, "Feature flag updated."));
    }

    [HttpPost("notifications/campaign")]
    public async Task<ActionResult<ApiResponse<NotificationCampaignResultDto>>> SendCampaign(
        NotificationCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.SendCampaignAsync(GetCurrentUserRole(), request, cancellationToken);
        return Ok(ApiResponse<NotificationCampaignResultDto>.Success(result, "Notification campaign queued."));
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    }
}
