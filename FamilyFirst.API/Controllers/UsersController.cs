using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.User;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(GetCurrentUserId(), userId, cancellationToken);

        return Ok(ApiResponse<UserDto>.Success(user));
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateUserAsync(GetCurrentUserId(), userId, request, cancellationToken);

        return Ok(ApiResponse<UserDto>.Success(user, "User updated."));
    }

    [HttpPut("{userId:guid}/fcm-token")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateFcmToken(
        Guid userId,
        FcmTokenRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateFcmTokenAsync(GetCurrentUserId(), userId, request, cancellationToken);

        return Ok(ApiResponse<bool>.Success(updated, "FCM token updated."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
