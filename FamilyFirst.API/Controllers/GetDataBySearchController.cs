using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.API.Controllers;

/// <summary>
/// Generic list/search endpoint for the entire project.
/// Replaces individual GET list endpoints across all modules.
///
/// Flow:
///   1. UI sends { ModuleCode, MethodName, SearchWord?, FromDate?, ToDate?, PageNumber, PageSize }
///   2. Controller resolves JWT context (UserId, FamilyId, Role)
///   3. Service looks up tblStaticAPITemplate → gets SP name
///   4. Repository executes SP dynamically with standard parameters
///   5. Returns paginated result as ApiResponse{StaticDataResponse}
///
/// Example request:
///   POST /api/GetDataBySearch
///   { "moduleCode": "ATTEND", "methodName": "GetAttendanceSessionBySearch",
///     "fromDate": "2026-06-01", "toDate": "2026-06-30", "pageNumber": 1, "pageSize": 10 }
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class GetDataBySearchController : ControllerBase
{
    private readonly IStaticDataService _staticDataService;
    private readonly ILogger<GetDataBySearchController> _logger;

    public GetDataBySearchController(
        IStaticDataService staticDataService,
        ILogger<GetDataBySearchController> logger)
    {
        _staticDataService = staticDataService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StaticDataResponse>>> GetDataBySearch(
        [FromBody] StaticSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{Controller}] Step 1 — Request received. ModuleCode={ModuleCode} MethodName={MethodName}",
            nameof(GetDataBySearchController), request.ModuleCode, request.MethodName);

        var currentUserId  = GetCurrentUserId();
        var currentFamilyId = GetCurrentFamilyId();
        var currentRole    = GetCurrentRole();

        _logger.LogDebug("[{Controller}] Step 2 — Calling service. UserId={UserId} FamilyId={FamilyId} Role={Role}",
            nameof(GetDataBySearchController), currentUserId, currentFamilyId, currentRole);

        var result = await _staticDataService.GetDataBySearchAsync(
            currentUserId,
            currentFamilyId,
            currentRole,
            request,
            cancellationToken);

        _logger.LogDebug("[{Controller}] Step 3 — Response ready. TotalCount={TotalCount}",
            nameof(GetDataBySearchController), result.TotalCount);

        return Ok(ApiResponse<StaticDataResponse>.Success(result));
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
