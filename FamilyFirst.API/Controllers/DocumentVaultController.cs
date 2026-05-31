using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Vault;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Route("api")]
public sealed class DocumentVaultController : ControllerBase
{
    private readonly IDocumentVaultService _vaultService;

    public DocumentVaultController(IDocumentVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/vault/documents/upload-url")]
    public async Task<ActionResult<ApiResponse<VaultUploadUrlDto>>> GetUploadUrl(
        Guid familyId,
        VaultUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetUploadUrlAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<VaultUploadUrlDto>.Success(result));
    }

    [Authorize]
    [HttpGet("families/{familyId:guid}/vault/documents")]
    public async Task<ActionResult<ApiResponse<PaginatedList<DocumentDto>>>> ListDocuments(
        Guid familyId,
        [FromQuery] string? category,
        [FromQuery] Guid? memberId,
        [FromQuery] string? search,
        [FromQuery] string? expiryStatus,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _vaultService.ListDocumentsAsync(
            GetCurrentUserId(), familyId, category, memberId, search,
            expiryStatus, dateFrom, dateTo, sortBy, page, pageSize, cancellationToken);

        return Ok(ApiResponse<PaginatedList<DocumentDto>>.Success(result));
    }

    [Authorize]
    [HttpGet("families/{familyId:guid}/vault/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDetailDto>>> GetDocument(
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetDocumentAsync(GetCurrentUserId(), familyId, documentId, cancellationToken);
        return Ok(ApiResponse<DocumentDetailDto>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/vault/documents")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> CreateDocument(
        Guid familyId,
        CreateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.CreateDocumentAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Created(
            $"/api/families/{familyId}/vault/documents/{result.DocumentId}",
            ApiResponse<DocumentDto>.Success(result, "Document uploaded successfully."));
    }

    [Authorize]
    [HttpPut("families/{familyId:guid}/vault/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> UpdateDocument(
        Guid familyId,
        Guid documentId,
        UpdateVaultDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.UpdateDocumentAsync(
            GetCurrentUserId(), familyId, documentId, request, cancellationToken);
        return Ok(ApiResponse<DocumentDto>.Success(result, "Document updated."));
    }

    [Authorize]
    [HttpDelete("families/{familyId:guid}/vault/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDocument(
        Guid familyId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _vaultService.DeleteDocumentAsync(GetCurrentUserId(), familyId, documentId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Document deleted. Recovery window: 30 days."));
    }

    [Authorize]
    [HttpGet("families/{familyId:guid}/vault/expiry")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DocumentDto>>>> GetExpiringDocuments(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetExpiringDocumentsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<DocumentDto>>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/vault/emergency")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DocumentDto>>>> GetEmergencyDocuments(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetEmergencyDocumentsAsync(familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<DocumentDto>>.Success(result));
    }

    [Authorize]
    [HttpPost("families/{familyId:guid}/vault/documents/{documentId:guid}/share")]
    public async Task<ActionResult<ApiResponse<ShareLinkDto>>> CreateShareLink(
        Guid familyId,
        Guid documentId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.CreateShareLinkAsync(
            GetCurrentUserId(), familyId, documentId, request, cancellationToken);
        return Created(result.ShareUrl, ApiResponse<ShareLinkDto>.Success(result, "Share link created."));
    }

    [Authorize]
    [HttpDelete("families/{familyId:guid}/vault/documents/{documentId:guid}/share/{shareLinkId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeShareLink(
        Guid familyId,
        Guid documentId,
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        await _vaultService.RevokeShareLinkAsync(
            GetCurrentUserId(), familyId, documentId, shareLinkId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Share link revoked."));
    }

    [Authorize]
    [HttpGet("families/{familyId:guid}/vault/settings")]
    public async Task<ActionResult<ApiResponse<VaultFamilySettingsDto>>> GetVaultSettings(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetVaultSettingsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<VaultFamilySettingsDto>.Success(result));
    }

    [Authorize]
    [HttpPut("families/{familyId:guid}/vault/settings")]
    public async Task<ActionResult<ApiResponse<VaultFamilySettingsDto>>> UpdateVaultSettings(
        Guid familyId,
        UpdateVaultFamilySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.UpdateVaultSettingsAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<VaultFamilySettingsDto>.Success(result, "Vault settings updated."));
    }

    [HttpGet("vault/share/{token}")]
    public async Task<ActionResult<ApiResponse<DocumentDetailDto>>> GetDocumentByShareToken(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _vaultService.GetDocumentByShareTokenAsync(token, cancellationToken);
        return Ok(ApiResponse<DocumentDetailDto>.Success(result));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
