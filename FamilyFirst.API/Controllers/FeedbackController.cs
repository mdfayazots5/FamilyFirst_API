using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Feedback;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> SubmitFeedback(
        Guid familyId,
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.SubmitFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);

        return Created(
            $"/api/families/{familyId}/feedback/{feedback.FeedbackId}",
            ApiResponse<FeedbackDto>.Success(feedback, "Feedback submitted."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<FeedbackDto>>>> ListFeedback(
        Guid familyId,
        [FromQuery] Guid? childId,
        [FromQuery] FeedbackType? type,
        [FromQuery] bool? isAcknowledged,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var feedback = await _feedbackService.ListFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            childId,
            type,
            isAcknowledged,
            page,
            pageSize,
            cancellationToken);

        return Ok(ApiResponse<PaginatedList<FeedbackDto>>.Success(feedback));
    }

    [HttpGet("{feedbackId:guid}")]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> GetFeedback(
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.GetFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            feedbackId,
            cancellationToken);

        return Ok(ApiResponse<FeedbackDto>.Success(feedback));
    }

    [HttpPut("{feedbackId:guid}")]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> UpdateFeedback(
        Guid familyId,
        Guid feedbackId,
        UpdateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.UpdateFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            feedbackId,
            request,
            cancellationToken);

        return Ok(ApiResponse<FeedbackDto>.Success(feedback, "Feedback updated."));
    }

    [HttpDelete("{feedbackId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFeedback(
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken)
    {
        var deleted = await _feedbackService.DeleteFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            feedbackId,
            cancellationToken);

        return Ok(ApiResponse<bool>.Success(deleted, "Feedback deleted."));
    }

    [HttpPost("{feedbackId:guid}/acknowledge")]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> AcknowledgeFeedback(
        Guid familyId,
        Guid feedbackId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.AcknowledgeFeedbackAsync(
            GetCurrentUserId(),
            familyId,
            feedbackId,
            request,
            cancellationToken);

        return Ok(ApiResponse<FeedbackDto>.Success(feedback, "Feedback acknowledged."));
    }

    [HttpGet("~/api/families/{familyId:guid}/children/{childId:guid}/feedback-summary")]
    public async Task<ActionResult<ApiResponse<FeedbackSummaryDto>>> GetFeedbackSummary(
        Guid familyId,
        Guid childId,
        [FromQuery] int periodDays = 7,
        CancellationToken cancellationToken = default)
    {
        var summary = await _feedbackService.GetFeedbackSummaryAsync(
            GetCurrentUserId(),
            familyId,
            childId,
            periodDays,
            cancellationToken);

        return Ok(ApiResponse<FeedbackSummaryDto>.Success(summary));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
