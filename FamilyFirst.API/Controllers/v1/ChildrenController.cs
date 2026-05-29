using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskDeductCoinsRequest = FamilyFirst.Application.DTOs.Task.DeductCoinsRequest;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/families/{familyId:guid}")]
public sealed class ChildrenController : ControllerBase
{
    private readonly IChildService _childService;
    private readonly ITeacherService _teacherService;

    public ChildrenController(
        IChildService childService,
        ITeacherService teacherService)
    {
        _childService = childService;
        _teacherService = teacherService;
    }

    [HttpGet("children")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ChildSummaryDto>>>> ListChildren(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var children = await _childService.ListChildrenAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<ChildSummaryDto>>.Success(children));
    }

    [HttpGet("children/{childId:guid}")]
    public async Task<ActionResult<ApiResponse<ChildDetailDto>>> GetChild(
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var child = await _childService.GetChildAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            cancellationToken);

        return Ok(ApiResponse<ChildDetailDto>.Success(child));
    }

    [HttpPut("children/{childId:guid}")]
    public async Task<ActionResult<ApiResponse<ChildDetailDto>>> UpdateChild(
        Guid familyId,
        Guid childId,
        UpdateChildRequest request,
        CancellationToken cancellationToken)
    {
        var child = await _childService.UpdateChildAsync(GetCurrentUserId(), familyId, childId, request, cancellationToken);

        return Ok(ApiResponse<ChildDetailDto>.Success(child, "Child profile updated."));
    }

    [HttpGet("children/{childId:guid}/score-history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ScoreHistoryDto>>>> GetScoreHistory(
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var scoreHistory = await _childService.GetScoreHistoryAsync(GetCurrentUserId(), familyId, childId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<ScoreHistoryDto>>.Success(scoreHistory));
    }

    [HttpPost("children/{childId:guid}/coin-deduction")]
    public async Task<ActionResult<ApiResponse<CoinTransactionDto>>> DeductCoins(
        Guid familyId,
        Guid childId,
        TaskDeductCoinsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _childService.DeductCoinsAsync(GetCurrentUserId(), familyId, childId, request, cancellationToken);

        return Ok(ApiResponse<CoinTransactionDto>.Success(result, "Coins deducted."));
    }

    [HttpGet("children/{childId:guid}/coin-history")]
    public async Task<ActionResult<ApiResponse<PaginatedList<CoinTransactionDto>>>> GetCoinHistory(
        Guid familyId,
        Guid childId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _childService.GetCoinHistoryAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(ApiResponse<PaginatedList<CoinTransactionDto>>.Success(history));
    }

    [HttpPost("children/{childId:guid}/streak/use-freeze")]
    public async Task<ActionResult<ApiResponse<bool>>> UseStreakFreeze(
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var used = await _childService.UseStreakFreezeAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            cancellationToken);

        return Ok(ApiResponse<bool>.Success(used, "Streak freeze used."));
    }

    [HttpPost("teachers/{teacherId:guid}/assign/{childId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignTeacher(
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var assigned = await _teacherService.AssignTeacherAsync(GetCurrentUserId(), familyId, teacherId, childId, cancellationToken);

        return Ok(ApiResponse<bool>.Success(assigned, "Teacher assigned."));
    }

    [HttpDelete("teachers/{teacherId:guid}/assign/{childId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UnassignTeacher(
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var unassigned = await _teacherService.UnassignTeacherAsync(GetCurrentUserId(), familyId, teacherId, childId, cancellationToken);

        return Ok(ApiResponse<bool>.Success(unassigned, "Teacher unassigned."));
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
