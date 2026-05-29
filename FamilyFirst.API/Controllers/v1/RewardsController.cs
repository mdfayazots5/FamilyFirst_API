using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class RewardsController : ControllerBase
{
    private readonly IRewardService _rewardService;

    public RewardsController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    [HttpGet("admin/rewards/catalog")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RewardDto>>>> ListSystemRewards(
        CancellationToken cancellationToken)
    {
        var rewards = await _rewardService.ListSystemRewardsAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<RewardDto>>.Success(rewards));
    }

    [HttpPost("admin/rewards/catalog")]
    public async Task<ActionResult<ApiResponse<RewardDto>>> CreateSystemReward(
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var reward = await _rewardService.CreateSystemRewardAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            request,
            cancellationToken);

        return Created(
            $"/api/v1/admin/rewards/catalog/{reward.RewardId}",
            ApiResponse<RewardDto>.Success(reward, "System reward created."));
    }

    [HttpPut("admin/rewards/catalog/{rewardId:guid}")]
    public async Task<ActionResult<ApiResponse<RewardDto>>> UpdateSystemReward(
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var reward = await _rewardService.UpdateSystemRewardAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            rewardId,
            request,
            cancellationToken);

        return Ok(ApiResponse<RewardDto>.Success(reward, "System reward updated."));
    }

    [HttpGet("families/{familyId:guid}/rewards")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RewardDto>>>> ListFamilyRewards(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var rewards = await _rewardService.ListFamilyRewardsAsync(
            GetCurrentUserId(),
            familyId,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<RewardDto>>.Success(rewards));
    }

    [HttpPost("families/{familyId:guid}/rewards")]
    public async Task<ActionResult<ApiResponse<RewardDto>>> CreateFamilyReward(
        Guid familyId,
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var reward = await _rewardService.CreateFamilyRewardAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);

        return Created(
            $"/api/v1/families/{familyId}/rewards/{reward.RewardId}",
            ApiResponse<RewardDto>.Success(reward, "Family reward created."));
    }

    [HttpPut("families/{familyId:guid}/rewards/{rewardId:guid}")]
    public async Task<ActionResult<ApiResponse<RewardDto>>> UpdateFamilyReward(
        Guid familyId,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var reward = await _rewardService.UpdateFamilyRewardAsync(
            GetCurrentUserId(),
            familyId,
            rewardId,
            request,
            cancellationToken);

        return Ok(ApiResponse<RewardDto>.Success(reward, "Family reward updated."));
    }

    [HttpPost("families/{familyId:guid}/rewards/{rewardId:guid}/redeem")]
    public async Task<ActionResult<ApiResponse<RedemptionDto>>> RedeemReward(
        Guid familyId,
        Guid rewardId,
        RedeemRequest request,
        CancellationToken cancellationToken)
    {
        var redemption = await _rewardService.RedeemAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            rewardId,
            request,
            cancellationToken);

        return Created(
            $"/api/v1/families/{familyId}/rewards/redemptions/{redemption.RedemptionId}",
            ApiResponse<RedemptionDto>.Success(redemption, "Reward redemption requested."));
    }

    [HttpGet("families/{familyId:guid}/rewards/redemptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RedemptionDto>>>> ListRedemptions(
        Guid familyId,
        [FromQuery] Guid? childId,
        [FromQuery] RedemptionStatus? status,
        CancellationToken cancellationToken)
    {
        var redemptions = await _rewardService.ListRedemptionsAsync(
            GetCurrentUserId(),
            familyId,
            childId,
            status,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<RedemptionDto>>.Success(redemptions));
    }

    [HttpPut("families/{familyId:guid}/rewards/redemptions/{redemptionId:guid}")]
    public async Task<ActionResult<ApiResponse<RedemptionDto>>> ReviewRedemption(
        Guid familyId,
        Guid redemptionId,
        ReviewRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        var redemption = await _rewardService.ReviewRedemptionAsync(
            GetCurrentUserId(),
            familyId,
            redemptionId,
            request,
            cancellationToken);

        return Ok(ApiResponse<RedemptionDto>.Success(redemption, "Redemption reviewed."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    }

    private Guid? GetCurrentChildProfileId()
    {
        var childProfileId = User.FindFirstValue("childProfileId");

        return Guid.TryParse(childProfileId, out var parsedChildProfileId) ? parsedChildProfileId : null;
    }
}
