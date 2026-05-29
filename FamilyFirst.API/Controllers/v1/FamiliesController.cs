using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/families")]
public sealed class FamiliesController : ControllerBase
{
    private readonly IFamilyService _familyService;

    public FamiliesController(IFamilyService familyService)
    {
        _familyService = familyService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FamilyDto>>> CreateFamily(
        CreateFamilyRequest request,
        CancellationToken cancellationToken)
    {
        var family = await _familyService.CreateFamilyAsync(GetCurrentUserId(), request, cancellationToken);

        return Created($"/api/v1/families/{family.FamilyId}", ApiResponse<FamilyDto>.Success(family, "Family created."));
    }

    [HttpGet("{familyId:guid}")]
    public async Task<ActionResult<ApiResponse<FamilyDto>>> GetFamily(Guid familyId, CancellationToken cancellationToken)
    {
        var family = await _familyService.GetFamilyAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<FamilyDto>.Success(family));
    }

    [HttpPut("{familyId:guid}")]
    public async Task<ActionResult<ApiResponse<FamilyDto>>> UpdateFamily(
        Guid familyId,
        UpdateFamilyRequest request,
        CancellationToken cancellationToken)
    {
        var family = await _familyService.UpdateFamilyAsync(GetCurrentUserId(), familyId, request, cancellationToken);

        return Ok(ApiResponse<FamilyDto>.Success(family, "Family updated."));
    }

    [HttpGet("{familyId:guid}/join-code")]
    public async Task<ActionResult<ApiResponse<object>>> GetJoinCode(Guid familyId, CancellationToken cancellationToken)
    {
        var joinCode = await _familyService.GetJoinCodeAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<object>.Success(new { JoinCode = joinCode }));
    }

    [HttpPost("{familyId:guid}/join-code/regenerate")]
    public async Task<ActionResult<ApiResponse<object>>> RegenerateJoinCode(Guid familyId, CancellationToken cancellationToken)
    {
        var joinCode = await _familyService.RegenerateJoinCodeAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<object>.Success(new { JoinCode = joinCode }, "Join code regenerated."));
    }

    [HttpPost("join")]
    public async Task<ActionResult<ApiResponse<FamilyMemberDto>>> JoinFamily(
        JoinFamilyRequest request,
        CancellationToken cancellationToken)
    {
        var familyMember = await _familyService.JoinFamilyAsync(GetCurrentUserId(), request, cancellationToken);

        return Ok(ApiResponse<FamilyMemberDto>.Success(familyMember, "Joined family."));
    }

    [HttpGet("{familyId:guid}/members")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<FamilyMemberDto>>>> ListMembers(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var familyMembers = await _familyService.ListMembersAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<FamilyMemberDto>>.Success(familyMembers));
    }

    [HttpPost("{familyId:guid}/members")]
    public async Task<ActionResult<ApiResponse<FamilyMemberDto>>> AddMember(
        Guid familyId,
        AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        var familyMember = await _familyService.AddMemberAsync(GetCurrentUserId(), familyId, request, cancellationToken);

        return Ok(ApiResponse<FamilyMemberDto>.Success(familyMember, "Member added."));
    }

    [HttpPut("{familyId:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<FamilyMemberDto>>> UpdateMember(
        Guid familyId,
        Guid memberId,
        UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var familyMember = await _familyService.UpdateMemberAsync(GetCurrentUserId(), familyId, memberId, request, cancellationToken);

        return Ok(ApiResponse<FamilyMemberDto>.Success(familyMember, "Member updated."));
    }

    [HttpDelete("{familyId:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var removed = await _familyService.RemoveMemberAsync(GetCurrentUserId(), familyId, memberId, cancellationToken);

        return Ok(ApiResponse<bool>.Success(removed, "Member removed."));
    }

    [HttpGet("{familyId:guid}/dashboard")]
    public async Task<ActionResult<ApiResponse<FamilyDashboardDto>>> GetDashboard(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var dashboard = await _familyService.GetDashboardAsync(GetCurrentUserId(), familyId, cancellationToken);

        return Ok(ApiResponse<FamilyDashboardDto>.Success(dashboard));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
