using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Finance;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IFinanceService
{
    // ── Dashboard ──────────────────────────────────────────────────────────────
    Task<FinanceDashboardDto> GetDashboardAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    // ── Transactions ───────────────────────────────────────────────────────────
    Task<PaginatedList<TransactionDto>> ListTransactionsAsync(
        Guid currentUserId, Guid familyId,
        Guid? memberId, string? category,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize,
        CancellationToken cancellationToken);

    Task<PaginatedList<TransactionDto>> ListMemberTransactionsAsync(
        Guid currentUserId, Guid familyId, Guid memberId,
        int page, int pageSize,
        CancellationToken cancellationToken);

    Task<TransactionQuestionDto> QuestionTransactionAsync(
        Guid currentUserId, Guid familyId, Guid transactionId,
        QuestionTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionQuestionDto?> GetTransactionQuestionAsync(
        Guid currentUserId, Guid familyId, Guid transactionId,
        CancellationToken cancellationToken);

    // ── Budget ─────────────────────────────────────────────────────────────────
    Task<IReadOnlyCollection<BudgetDto>> GetBudgetsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<BudgetDto> SetBudgetAsync(
        Guid currentUserId, Guid familyId,
        SetBudgetRequest request, CancellationToken cancellationToken);

    // ── Category Breakdown ─────────────────────────────────────────────────────
    Task<IReadOnlyCollection<CategorySpendDto>> GetCategoryBreakdownAsync(
        Guid currentUserId, Guid familyId,
        DateTime? fromDate, DateTime? toDate,
        CancellationToken cancellationToken);

    // ── Commitments ────────────────────────────────────────────────────────────
    Task<IReadOnlyCollection<CommitmentDto>> ListCommitmentsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    // ── Consent ────────────────────────────────────────────────────────────────
    Task<ConsentInviteDto> InviteConsentAsync(
        Guid currentUserId, Guid familyId,
        InviteConsentRequest request, CancellationToken cancellationToken);

    Task AcceptConsentAsync(
        Guid familyId, AcceptFinanceConsentRequest request,
        string ipAddress, CancellationToken cancellationToken);

    Task DeclineConsentAsync(
        Guid familyId, string consentToken, CancellationToken cancellationToken);

    Task RevokeConsentAsync(
        Guid currentUserId, Guid familyId, Guid memberId,
        CancellationToken cancellationToken);

    // ── Settings ───────────────────────────────────────────────────────────────
    Task<FinanceSettingsDto> GetSettingsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<FinanceSettingsDto> UpdateSettingsAsync(
        Guid currentUserId, Guid familyId,
        UpdateFinanceSettingsRequest request, CancellationToken cancellationToken);
}

public interface IFinanceRepository
{
    // Consent
    Task<FinanceConsent?> GetConsentByMemberAsync(Guid familyMemberId, CancellationToken cancellationToken);
    Task<FinanceConsent?> GetConsentByTokenAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FinanceConsent>> ListConsentsByFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    Task<FinanceConsent> AddConsentAsync(FinanceConsent consent, CancellationToken cancellationToken);
    Task UpdateConsentAsync(FinanceConsent consent, CancellationToken cancellationToken);

    // Transactions
    Task<(IReadOnlyCollection<Transaction> Items, int TotalCount)> ListTransactionsAsync(
        Guid familyId, Guid? memberId, string? category,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken cancellationToken);

    Task<Transaction?> GetTransactionByIdAsync(Guid transactionId, Guid familyId, CancellationToken cancellationToken);
    Task<Transaction> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken);
    Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken);
    Task PurgeOptOutTransactionsAsync(Guid familyMemberId, CancellationToken cancellationToken);

    // Transaction Questions
    Task<TransactionQuestion?> GetQuestionByTransactionAsync(Guid transactionId, CancellationToken cancellationToken);
    Task<TransactionQuestion> AddQuestionAsync(TransactionQuestion question, CancellationToken cancellationToken);
    Task UpdateQuestionAsync(TransactionQuestion question, CancellationToken cancellationToken);

    // Budgets
    Task<IReadOnlyCollection<Budget>> ListBudgetsAsync(Guid familyId, DateOnly monthYear, CancellationToken cancellationToken);
    Task<Budget?> GetBudgetAsync(Guid familyId, string category, DateOnly monthYear, CancellationToken cancellationToken);
    Task UpsertBudgetAsync(Budget budget, CancellationToken cancellationToken);

    // Category aggregates
    Task<IReadOnlyCollection<(string Category, decimal Total, int Count, string? TopMerchant)>> GetCategorySpendAsync(
        Guid familyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);

    // Commitments
    Task<IReadOnlyCollection<Commitment>> ListCommitmentsAsync(Guid familyId, CancellationToken cancellationToken);
    Task<Commitment> AddCommitmentAsync(Commitment commitment, CancellationToken cancellationToken);
    Task UpdateCommitmentAsync(Commitment commitment, CancellationToken cancellationToken);

    // Finance Settings
    Task<FinanceSetting?> GetSettingsAsync(Guid familyId, CancellationToken cancellationToken);
    Task UpsertSettingsAsync(FinanceSetting settings, CancellationToken cancellationToken);

    // MTD aggregates for dashboard
    Task<decimal> GetTotalSpendMtdAsync(Guid familyId, DateTime monthStart, CancellationToken cancellationToken);
    Task<decimal> GetTotalIncomeMtdAsync(Guid familyId, DateTime monthStart, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Transaction>> GetTodaysTransactionsAsync(Guid familyId, DateTime dayStart, CancellationToken cancellationToken);
}
