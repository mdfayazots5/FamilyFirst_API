using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/users/{userId:guid}/notification-preferences")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationPreferenceService _notificationPreferenceService;

    public NotificationsController(INotificationPreferenceService notificationPreferenceService)
    {
        _notificationPreferenceService = notificationPreferenceService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preference = await _notificationPreferenceService.GetPreferencesAsync(
            GetCurrentUserId(),
            userId,
            cancellationToken);

        return Ok(ApiResponse<NotificationPreferenceDto>.Success(preference));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> UpdatePreferences(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var preference = await _notificationPreferenceService.UpdatePreferencesAsync(
            GetCurrentUserId(),
            userId,
            request,
            cancellationToken);

        return Ok(ApiResponse<NotificationPreferenceDto>.Success(preference, "Notification preferences updated."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
