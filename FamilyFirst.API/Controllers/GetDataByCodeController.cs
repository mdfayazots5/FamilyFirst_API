using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.API.Controllers;

/// <summary>
/// Generic single-record (by GUID) endpoint for the entire project.
/// Replaces individual GET-by-Id endpoints across all modules.
///
/// Flow:
///   1. UI sends { ModuleCode, MethodName, Id (GUID of the record) }
///   2. Controller resolves JWT context (UserId, FamilyId, Role)
///   3. Service looks up tblStaticAPITemplate → gets SP name
///   4. Repository executes SP dynamically; SP uses @Id parameter to find the record
///   5. Returns single record as ApiResponse{Dictionary}
///      404 NotFoundException if record not found or FamilyId mismatch (security)
///
/// Example request:
///   POST /api/GetDataByCode
///   { "moduleCode": "ATTEND", "methodName": "GetAttendanceSessionById",
///     "id": "A1B2C3D4-E5F6-7890-ABCD-EF1234567890" }
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class GetDataByCodeController : ControllerBase
{
    private readonly IStaticDataService _staticDataService;
    private readonly ILogger<GetDataByCodeController> _logger;

    public GetDataByCodeController(
        IStaticDataService staticDataService,
        ILogger<GetDataByCodeController> logger)
    {
        _staticDataService = staticDataService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IReadOnlyDictionary<string, object?>>>> GetDataByCode(
        [FromBody] StaticCodeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{Controller}] Step 1 — Request received. ModuleCode={ModuleCode} MethodName={MethodName} Id={Id}",
            nameof(GetDataByCodeController), request.ModuleCode, request.MethodName, request.Id);

        var currentUserId   = GetCurrentUserId();
        var currentFamilyId = GetCurrentFamilyId();
        var currentRole     = GetCurrentRole();

        _logger.LogDebug("[{Controller}] Step 2 — Calling service. UserId={UserId} FamilyId={FamilyId} Role={Role}",
            nameof(GetDataByCodeController), currentUserId, currentFamilyId, currentRole);

        var result = await _staticDataService.GetDataByCodeAsync(
            currentUserId,
            currentFamilyId,
            currentRole,
            request,
            cancellationToken);

        _logger.LogDebug("[{Controller}] Step 3 — Record found", nameof(GetDataByCodeController));

        return Ok(ApiResponse<IReadOnlyDictionary<string, object?>>.Success(result));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var id) ? id : Guid.Empty;
    }

    private Guid? GetCurrentFamilyId()
    {
        var familyIdClaim = User.FindFirstValue("familyId");

        return Guid.TryParse(familyIdClaim, out var id) ? id : null;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role")
            ?? string.Empty;
    }

    private static class JwtRegisteredClaimNames
    {
        public const string Sub = "sub";
    }
}
