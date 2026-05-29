using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/families/{familyId:guid}/comment-templates")]
public sealed class CommentTemplatesController : ControllerBase
{
    private readonly ICommentTemplateService _commentTemplateService;

    public CommentTemplatesController(ICommentTemplateService commentTemplateService)
    {
        _commentTemplateService = commentTemplateService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CommentTemplateDto>>>> ListTemplates(
        Guid familyId,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var templates = await _commentTemplateService.ListTemplatesAsync(
            GetCurrentUserId(),
            familyId,
            category,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<CommentTemplateDto>>.Success(templates));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CommentTemplateDto>>> CreateTemplate(
        Guid familyId,
        CreateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _commentTemplateService.CreateTemplateAsync(
            GetCurrentUserId(),
            familyId,
            request,
            cancellationToken);

        return Created(
            $"/api/v1/families/{familyId}/comment-templates/{template.TemplateId}",
            ApiResponse<CommentTemplateDto>.Success(template, "Comment template created."));
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<ApiResponse<CommentTemplateDto>>> UpdateTemplate(
        Guid familyId,
        Guid templateId,
        UpdateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _commentTemplateService.UpdateTemplateAsync(
            GetCurrentUserId(),
            familyId,
            templateId,
            request,
            cancellationToken);

        return Ok(ApiResponse<CommentTemplateDto>.Success(template, "Comment template updated."));
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplate(
        Guid familyId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var deleted = await _commentTemplateService.DeleteTemplateAsync(
            GetCurrentUserId(),
            familyId,
            templateId,
            cancellationToken);

        return Ok(ApiResponse<bool>.Success(deleted, "Comment template deleted."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
