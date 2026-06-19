using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/notification-preferences")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _notificationPreferenceService;

    public NotificationsController(
        INotificationService notificationService,
        INotificationPreferenceService notificationPreferenceService)
    {
        _notificationService = notificationService;
        _notificationPreferenceService = notificationPreferenceService;
    }

    [HttpGet("/api/users/{userId:guid}/notifications")]
    public async Task<ActionResult<ApiResponse<PaginatedList<NotificationDto>>>> ListNotifications(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationService.ListNotificationsAsync(
            GetCurrentUserId(),
            userId,
            page,
            pageSize,
            isRead,
            cancellationToken);

        return Ok(ApiResponse<PaginatedList<NotificationDto>>.Success(notifications));
    }

    [HttpPut("/api/users/{userId:guid}/notifications/{notificationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkRead(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var marked = await _notificationService.MarkReadAsync(
            GetCurrentUserId(),
            userId,
            notificationId,
            cancellationToken);

        return Ok(ApiResponse<bool>.Success(marked, "Notification marked as read."));
    }

    [HttpPut("/api/users/{userId:guid}/notifications/read-all")]
    public async Task<ActionResult<ApiResponse<MarkAllReadResultDto>>> MarkAllRead(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAllReadAsync(
            GetCurrentUserId(),
            userId,
            cancellationToken);

        return Ok(ApiResponse<MarkAllReadResultDto>.Success(result, "Notifications marked as read."));
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
