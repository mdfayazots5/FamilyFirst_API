using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Finance;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/dashboard")]
    public async Task<ActionResult<ApiResponse<FinanceDashboardDto>>> GetDashboard(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _financeService.GetDashboardAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<FinanceDashboardDto>.Success(result));
    }

    // ── Transactions ───────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/transactions")]
    public async Task<ActionResult<ApiResponse<PaginatedList<TransactionDto>>>> ListTransactions(
        Guid familyId,
        [FromQuery] Guid? memberId,
        [FromQuery] string? category,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _financeService.ListTransactionsAsync(
            GetCurrentUserId(), familyId, memberId, category, fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedList<TransactionDto>>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/finance/members/{memberId:guid}/transactions")]
    public async Task<ActionResult<ApiResponse<PaginatedList<TransactionDto>>>> ListMemberTransactions(
        Guid familyId,
        Guid memberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _financeService.ListMemberTransactionsAsync(
            GetCurrentUserId(), familyId, memberId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PaginatedList<TransactionDto>>.Success(result));
    }

    [HttpGet("families/{familyId:guid}/finance/transactions/{transactionId:guid}/question")]
    public async Task<ActionResult<ApiResponse<TransactionQuestionDto?>>> GetTransactionQuestion(
        Guid familyId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.GetTransactionQuestionAsync(
            GetCurrentUserId(), familyId, transactionId, cancellationToken);
        return Ok(ApiResponse<TransactionQuestionDto?>.Success(result));
    }

    [HttpPost("families/{familyId:guid}/finance/transactions/{transactionId:guid}/question")]
    public async Task<ActionResult<ApiResponse<TransactionQuestionDto>>> QuestionTransaction(
        Guid familyId,
        Guid transactionId,
        QuestionTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.QuestionTransactionAsync(
            GetCurrentUserId(), familyId, transactionId, request, cancellationToken);
        return Created(
            $"/api/v1/families/{familyId}/finance/transactions/{transactionId}",
            ApiResponse<TransactionQuestionDto>.Success(result, "Question sent."));
    }

    // ── Budget ─────────────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/budget")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BudgetDto>>>> GetBudgets(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _financeService.GetBudgetsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<BudgetDto>>.Success(result));
    }

    [HttpPut("families/{familyId:guid}/finance/budget")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> SetBudget(
        Guid familyId,
        SetBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.SetBudgetAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<BudgetDto>.Success(result, "Budget updated."));
    }

    // ── Category Breakdown ─────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CategorySpendDto>>>> GetCategoryBreakdown(
        Guid familyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.GetCategoryBreakdownAsync(
            GetCurrentUserId(), familyId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CategorySpendDto>>.Success(result));
    }

    // ── Commitments ────────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/commitments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CommitmentDto>>>> ListCommitments(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _financeService.ListCommitmentsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CommitmentDto>>.Success(result));
    }

    // ── Consent ────────────────────────────────────────────────────────────────

    [HttpPost("families/{familyId:guid}/finance/consent/invite")]
    public async Task<ActionResult<ApiResponse<ConsentInviteDto>>> InviteConsent(
        Guid familyId,
        InviteConsentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.InviteConsentAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<ConsentInviteDto>.Success(result, "Consent invite sent."));
    }

    // Public — accessible without auth (mobile web consent page)
    [AllowAnonymous]
    [HttpPost("families/{familyId:guid}/finance/consent/accept")]
    public async Task<ActionResult<ApiResponse<bool>>> AcceptConsent(
        Guid familyId,
        AcceptFinanceConsentRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _financeService.AcceptConsentAsync(familyId, request, ipAddress, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Consent accepted. Finance tracking is now active."));
    }

    // Public — accessible without auth (member declining from consent page)
    [AllowAnonymous]
    [HttpPost("families/{familyId:guid}/finance/consent/decline")]
    public async Task<ActionResult<ApiResponse<bool>>> DeclineConsent(
        Guid familyId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await _financeService.DeclineConsentAsync(familyId, token, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Consent declined."));
    }

    [HttpDelete("families/{familyId:guid}/finance/consent/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeConsent(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        await _financeService.RevokeConsentAsync(GetCurrentUserId(), familyId, memberId, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true, "Finance data sharing stopped. Data removed."));
    }

    // ── Settings ───────────────────────────────────────────────────────────────

    [HttpGet("families/{familyId:guid}/finance/settings")]
    public async Task<ActionResult<ApiResponse<FinanceSettingsDto>>> GetSettings(
        Guid familyId, CancellationToken cancellationToken)
    {
        var result = await _financeService.GetSettingsAsync(GetCurrentUserId(), familyId, cancellationToken);
        return Ok(ApiResponse<FinanceSettingsDto>.Success(result));
    }

    [HttpPut("families/{familyId:guid}/finance/settings")]
    public async Task<ActionResult<ApiResponse<FinanceSettingsDto>>> UpdateSettings(
        Guid familyId,
        UpdateFinanceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _financeService.UpdateSettingsAsync(GetCurrentUserId(), familyId, request, cancellationToken);
        return Ok(ApiResponse<FinanceSettingsDto>.Success(result, "Finance settings updated."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
