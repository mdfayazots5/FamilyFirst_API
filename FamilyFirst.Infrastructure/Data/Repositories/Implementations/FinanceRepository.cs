using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class FinanceRepository : IFinanceRepository
{
    private readonly FamilyFirstDbContext _db;

    public FinanceRepository(FamilyFirstDbContext db)
    {
        _db = db;
    }

    // ── Consent ────────────────────────────────────────────────────────────────

    public Task<FinanceConsent?> GetConsentByMemberAsync(Guid familyMemberId, CancellationToken ct) =>
        _db.Set<FinanceConsent>()
            .SingleOrDefaultAsync(c => c.FamilyMemberId == familyMemberId, ct);

    public Task<FinanceConsent?> GetConsentByTokenAsync(string token, CancellationToken ct) =>
        _db.Set<FinanceConsent>()
            .SingleOrDefaultAsync(c => c.ConsentToken == token, ct);

    public async Task<IReadOnlyCollection<FinanceConsent>> ListConsentsByFamilyAsync(Guid familyId, CancellationToken ct) =>
        await _db.Set<FinanceConsent>()
            .Include(c => c.FamilyMember)
            .Where(c => c.FamilyId == familyId)
            .ToArrayAsync(ct);

    public async Task<FinanceConsent> AddConsentAsync(FinanceConsent consent, CancellationToken ct)
    {
        _db.Set<FinanceConsent>().Add(consent);
        await _db.SaveChangesAsync(ct);
        return consent;
    }

    public async Task UpdateConsentAsync(FinanceConsent consent, CancellationToken ct)
    {
        _db.Set<FinanceConsent>().Update(consent);
        await _db.SaveChangesAsync(ct);
    }

    // ── Transactions ───────────────────────────────────────────────────────────

    public async Task<(IReadOnlyCollection<Transaction> Items, int TotalCount)> ListTransactionsAsync(
        Guid familyId, Guid? memberId, string? category,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<Transaction>()
            .Where(t => t.FamilyId == familyId);

        if (memberId.HasValue)  query = query.Where(t => t.FamilyMemberId == memberId.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(t => t.Category == category);
        if (fromDate.HasValue)  query = query.Where(t => t.ParsedAt >= fromDate.Value);
        if (toDate.HasValue)    query = query.Where(t => t.ParsedAt <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.ParsedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return (items, total);
    }

    public Task<Transaction?> GetTransactionByIdAsync(Guid transactionId, Guid familyId, CancellationToken ct) =>
        _db.Set<Transaction>()
            .SingleOrDefaultAsync(t => t.Id == transactionId && t.FamilyId == familyId, ct);

    public async Task<Transaction> AddTransactionAsync(Transaction transaction, CancellationToken ct)
    {
        _db.Set<Transaction>().Add(transaction);
        await _db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task UpdateTransactionAsync(Transaction transaction, CancellationToken ct)
    {
        _db.Set<Transaction>().Update(transaction);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PurgeOptOutTransactionsAsync(Guid familyMemberId, CancellationToken ct)
    {
        var transactions = await _db.Set<Transaction>()
            .Where(t => t.FamilyMemberId == familyMemberId)
            .ToArrayAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var t in transactions)
        {
            t.RawSmsText = null;            // sensitive — purge immediately
            t.IsDeleted  = true;
            t.DeletedAt  = now;
        }

        _db.Set<Transaction>().UpdateRange(transactions);
        await _db.SaveChangesAsync(ct);
    }

    // ── Transaction Questions ──────────────────────────────────────────────────

    public Task<TransactionQuestion?> GetQuestionByTransactionAsync(Guid transactionId, CancellationToken ct) =>
        _db.Set<TransactionQuestion>()
            .SingleOrDefaultAsync(q => q.TransactionId == transactionId, ct);

    public async Task<TransactionQuestion> AddQuestionAsync(TransactionQuestion question, CancellationToken ct)
    {
        _db.Set<TransactionQuestion>().Add(question);
        await _db.SaveChangesAsync(ct);
        return question;
    }

    public async Task UpdateQuestionAsync(TransactionQuestion question, CancellationToken ct)
    {
        _db.Set<TransactionQuestion>().Update(question);
        await _db.SaveChangesAsync(ct);
    }

    // ── Budgets ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<Budget>> ListBudgetsAsync(Guid familyId, DateOnly monthYear, CancellationToken ct) =>
        await _db.Set<Budget>()
            .Where(b => b.FamilyId == familyId && b.MonthYear == monthYear)
            .ToArrayAsync(ct);

    public Task<Budget?> GetBudgetAsync(Guid familyId, string category, DateOnly monthYear, CancellationToken ct) =>
        _db.Set<Budget>()
            .SingleOrDefaultAsync(b => b.FamilyId == familyId && b.Category == category && b.MonthYear == monthYear, ct);

    public async Task UpsertBudgetAsync(Budget budget, CancellationToken ct)
    {
        var existing = await _db.Set<Budget>()
            .SingleOrDefaultAsync(b => b.FamilyId == budget.FamilyId
                && b.Category == budget.Category && b.MonthYear == budget.MonthYear, ct);

        if (existing is null)
            _db.Set<Budget>().Add(budget);
        else
        {
            existing.BudgetAmount = budget.BudgetAmount;
            existing.SetByUserId  = budget.SetByUserId;
            existing.UpdatedAt    = DateTime.UtcNow;
            _db.Set<Budget>().Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Category aggregates ────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<(string Category, decimal Total, int Count, string? TopMerchant)>> GetCategorySpendAsync(
        Guid familyId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var groups = await _db.Set<Transaction>()
            .Where(t => t.FamilyId == familyId
                && t.TransactionType == "Debit"
                && t.ParsedAt >= fromDate && t.ParsedAt <= toDate)
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total    = g.Sum(t => t.Amount),
                Count    = g.Count()
            })
            .ToArrayAsync(ct);

        // Top merchant per category (Tier 1 only — hash not stored for Tier 2/3)
        var result = new List<(string, decimal, int, string?)>(groups.Length);
        foreach (var g in groups)
        {
            var topMerchant = await _db.Set<Transaction>()
                .Where(t => t.FamilyId == familyId
                    && t.Category == g.Category
                    && t.TransactionType == "Debit"
                    && t.ParsedAt >= fromDate && t.ParsedAt <= toDate
                    && t.MerchantName != null
                    && t.PrivacyTierAtCapture == PrivacyTier.FullVisibility)
                .GroupBy(t => t.MerchantName!)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .FirstOrDefaultAsync(ct);

            result.Add((g.Category, g.Total, g.Count, topMerchant));
        }

        return result;
    }

    // ── Commitments ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<Commitment>> ListCommitmentsAsync(Guid familyId, CancellationToken ct) =>
        await _db.Set<Commitment>()
            .Where(c => c.FamilyId == familyId)
            .OrderBy(c => c.NextDueDate)
            .ToArrayAsync(ct);

    public async Task<Commitment> AddCommitmentAsync(Commitment commitment, CancellationToken ct)
    {
        _db.Set<Commitment>().Add(commitment);
        await _db.SaveChangesAsync(ct);
        return commitment;
    }

    public async Task UpdateCommitmentAsync(Commitment commitment, CancellationToken ct)
    {
        _db.Set<Commitment>().Update(commitment);
        await _db.SaveChangesAsync(ct);
    }

    // ── Finance Settings ───────────────────────────────────────────────────────

    public Task<FinanceSetting?> GetSettingsAsync(Guid familyId, CancellationToken ct) =>
        _db.Set<FinanceSetting>()
            .SingleOrDefaultAsync(s => s.FamilyId == familyId, ct);

    public async Task UpsertSettingsAsync(FinanceSetting settings, CancellationToken ct)
    {
        var existing = await _db.Set<FinanceSetting>()
            .SingleOrDefaultAsync(s => s.FamilyId == settings.FamilyId, ct);

        if (existing is null)
            _db.Set<FinanceSetting>().Add(settings);
        else
        {
            existing.CfoFamilyMemberId = settings.CfoFamilyMemberId;
            existing.IsModuleEnabled   = settings.IsModuleEnabled;
            existing.EnabledAt         = settings.EnabledAt ?? existing.EnabledAt;
            existing.UpdatedAt         = DateTime.UtcNow;
            _db.Set<FinanceSetting>().Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── MTD aggregates ─────────────────────────────────────────────────────────

    public Task<decimal> GetTotalSpendMtdAsync(Guid familyId, DateTime monthStart, CancellationToken ct) =>
        _db.Set<Transaction>()
            .Where(t => t.FamilyId == familyId
                && t.TransactionType == "Debit"
                && t.ParsedAt >= monthStart)
            .SumAsync(t => (decimal?)t.Amount ?? 0m, ct);

    public Task<decimal> GetTotalIncomeMtdAsync(Guid familyId, DateTime monthStart, CancellationToken ct) =>
        _db.Set<Transaction>()
            .Where(t => t.FamilyId == familyId
                && t.TransactionType == "Credit"
                && t.ParsedAt >= monthStart)
            .SumAsync(t => (decimal?)t.Amount ?? 0m, ct);

    public async Task<IReadOnlyCollection<Transaction>> GetTodaysTransactionsAsync(
        Guid familyId, DateTime dayStart, CancellationToken ct) =>
        await _db.Set<Transaction>()
            .Where(t => t.FamilyId == familyId && t.ParsedAt >= dayStart)
            .OrderByDescending(t => t.ParsedAt)
            .ToArrayAsync(ct);
}
