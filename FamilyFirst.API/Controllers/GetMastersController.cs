using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.API.Controllers;

/// <summary>
/// Returns master data dropdown lists for any MasterDataCodes category.
///
/// Flow:
///   1. UI sends { MasterDataCode, Code?, SearchWord?, PageNumber, PageSize, LanguageId }
///   2. Controller resolves JWT context (UserId, FamilyId)
///   3. Service validates MasterDataCode against MasterDataCodes enum
///   4. Repository calls uspGetMasterDataByCode with standard params
///   5. Returns ApiResponse{GetMastersResponse} — Id (GUID), Name, Code, SortOrder only
///      No INT PKs are ever returned to the UI.
///
/// Usage examples:
///   POST /api/GetMasters  { "masterDataCode": "TaskType" }
///   POST /api/GetMasters  { "masterDataCode": "ChildProfile", "pageSize": 50 }
///   POST /api/GetMasters  { "masterDataCode": "AttendanceStatus", "searchWord": "Present" }
///   POST /api/GetMasters  { "masterDataCode": "FamilyMember", "code": "&lt;guid&gt;" }
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class GetMastersController : ControllerBase
{
    private readonly IStaticDataService _staticDataService;
    private readonly ILogger<GetMastersController> _logger;

    public GetMastersController(
        IStaticDataService staticDataService,
        ILogger<GetMastersController> logger)
    {
        _staticDataService = staticDataService;
        _logger            = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GetMastersResponse>>> GetMasters(
        [FromBody] GetMastersRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{Controller}] Step 1 — Request received. MasterDataCode={Code}",
            nameof(GetMastersController), request.MasterDataCode);

        var currentUserId   = GetCurrentUserId();
        var currentFamilyId = GetCurrentFamilyId();

        _logger.LogDebug("[{Controller}] Step 2 — Calling service. UserId={UserId} FamilyId={FamilyId}",
            nameof(GetMastersController), currentUserId, currentFamilyId);

        var result = await _staticDataService.GetMastersAsync(
            currentUserId,
            currentFamilyId,
            request,
            cancellationToken);

        _logger.LogDebug("[{Controller}] Step 3 — Response ready. TotalCount={TotalCount}",
            nameof(GetMastersController), result.TotalCount);

        return Ok(ApiResponse<GetMastersResponse>.Success(result));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var id) ? id : Guid.Empty;
    }

    private Guid? GetCurrentFamilyId()
    {
        var familyIdClaim = User.FindFirstValue("familyId");

        return Guid.TryParse(familyIdClaim, out var id) ? id : null;
    }
}
