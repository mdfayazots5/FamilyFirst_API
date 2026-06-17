using System.Security.Cryptography;
using System.Text;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Finance;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class FinanceService : IFinanceService
{
    private const string CurrentConsentVersion = "v1.0";

    private readonly IFinanceRepository       _financeRepository;
    private readonly IFamilyMemberRepository  _memberRepository;
    private readonly IUserRepository          _userRepository;
    private readonly INotificationRepository  _notificationRepository;

    public FinanceService(
        IFinanceRepository financeRepository,
        IFamilyMemberRepository memberRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _financeRepository     = financeRepository;
        _memberRepository      = memberRepository;
        _userRepository        = userRepository;
        _notificationRepository = notificationRepository;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────────

    public async Task<FinanceDashboardDto> GetDashboardAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);
        await EnsureModuleEnabledAsync(familyId, cancellationToken);

        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var dayStart   = now.Date.ToUniversalTime();

        var totalSpend  = await _financeRepository.GetTotalSpendMtdAsync(familyId, monthStart, cancellationToken);
        var totalIncome = await _financeRepository.GetTotalIncomeMtdAsync(familyId, monthStart, cancellationToken);
        var netSavings  = totalIncome - totalSpend;
        var savingsRate = totalIncome > 0 ? Math.Round(netSavings / totalIncome * 100m, 1) : 0m;
        var status      = totalIncome > 0 && totalSpend / totalIncome > 1.0m ? "Red"
                        : totalIncome > 0 && totalSpend / totalIncome > 0.8m ? "Amber"
                        : "Green";

        var consents = await _financeRepository.ListConsentsByFamilyAsync(familyId, cancellationToken);
        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var memberCards = new List<MemberSpendCardDto>();

        foreach (var consent in consents.Where(c => c.ConsentStatus == "Accepted"))
        {
            var member = allMembers.FirstOrDefault(m => m.InternalId == consent.FamilyMemberId);
            if (member is null) continue;

            var (today, month, isAbove) = await GetMemberSpendForCardAsync(
                familyId, member.Id, consent.PrivacyTier, monthStart, dayStart, cancellationToken);

            memberCards.Add(new MemberSpendCardDto(
                member.Id, member.DisplayName ?? string.Empty, null,
                consent.PrivacyTier, today, month,
                consent.PrivacyTier == PrivacyTier.AggregateOnly ? month : null,
                isAbove));
        }

        var todayTxns = await _financeRepository.GetTodaysTransactionsAsync(familyId, dayStart, cancellationToken);
        var todayDtos = todayTxns
            .Select(t => ApplyPrivacyFilter(t, GetConsentTierByInternalMemberId(consents, t.FamilyMemberId), allMembers))
            .Where(t => t is not null).Cast<TransactionDto>()
            .OrderByDescending(t => t.ParsedAt).ToArray();

        var commitments = await _financeRepository.ListCommitmentsAsync(familyId, cancellationToken);
        var alerts      = BuildAlerts(totalSpend, totalIncome, commitments.Where(c => c.Status == "Missed").ToArray());
        var upcoming    = commitments
            .Where(c => c.Status is "Upcoming" or "PendingConfirmation")
            .OrderBy(c => c.NextDueDate)
            .Take(3)
            .Select(c => MapToCommitmentDto(c, allMembers))
            .ToArray();

        return new FinanceDashboardDto(
            new FamilyHealthScoreDto(totalSpend, totalIncome, netSavings, savingsRate, status),
            memberCards,
            todayDtos,
            alerts,
            upcoming);
    }

    // ── Transactions ───────────────────────────────────────────────────────────

    public async Task<PaginatedList<TransactionDto>> ListTransactionsAsync(
        Guid currentUserId, Guid familyId,
        Guid? memberId, string? category,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize,
        CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);
        await EnsureModuleEnabledAsync(familyId, cancellationToken);

        var consents   = await _financeRepository.ListConsentsByFamilyAsync(familyId, cancellationToken);
        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var (items, total) = await _financeRepository.ListTransactionsAsync(
            familyId, memberId, category, fromDate, toDate, page, pageSize, cancellationToken);

        var dtos = items
            .Select(t => ApplyPrivacyFilter(t, GetConsentTierByInternalMemberId(consents, t.FamilyMemberId), allMembers))
            .Where(t => t is not null).Cast<TransactionDto>()
            .ToList();

        return new PaginatedList<TransactionDto>(dtos, total, page, pageSize);
    }

    public async Task<PaginatedList<TransactionDto>> ListMemberTransactionsAsync(
        Guid currentUserId, Guid familyId, Guid memberId,
        int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var currentMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        // CFO sees all consented members; own member sees own transactions
        var isCfo = await IsCfoAsync(currentUserId, familyId, cancellationToken);
        if (!isCfo && currentMember.Id != memberId)
            throw new ForbiddenAccessException("Only the Family CFO can view other members' transactions.");

        await EnsureConsentAsync(familyId, memberId, cancellationToken);

        var consents   = await _financeRepository.ListConsentsByFamilyAsync(familyId, cancellationToken);
        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var tier       = GetConsentTier(consents, memberId);

        var (items, total) = await _financeRepository.ListTransactionsAsync(
            familyId, memberId, null, null, null, page, pageSize, cancellationToken);

        var dtos = items
            .Select(t => ApplyPrivacyFilter(t, tier, allMembers))
            .Where(t => t is not null).Cast<TransactionDto>()
            .ToList();

        return new PaginatedList<TransactionDto>(dtos, total, page, pageSize);
    }

    public async Task<TransactionQuestionDto> QuestionTransactionAsync(
        Guid currentUserId, Guid familyId, Guid transactionId,
        QuestionTransactionRequest request, CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);

        var transaction = await _financeRepository.GetTransactionByIdAsync(transactionId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), transactionId);

        var question = new TransactionQuestion
        {
            FamilyId      = transaction.FamilyId,
            TransactionId = transaction.InternalId,
            QuestionType  = request.QuestionType,
            ContextNote   = request.ContextNote,
            MessageSentAt = DateTime.UtcNow
        };

        var created = await _financeRepository.AddQuestionAsync(question, cancellationToken);

        transaction.QuestionStatus = "Pending";
        await _financeRepository.UpdateTransactionAsync(transaction, cancellationToken);

        return new TransactionQuestionDto(
            created.Id, transactionId, request.QuestionType, request.ContextNote,
            created.MessageSentAt, null, null, null, null);
    }

    public async Task<TransactionQuestionDto?> GetTransactionQuestionAsync(
        Guid currentUserId, Guid familyId, Guid transactionId,
        CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);

        var transaction = await _financeRepository.GetTransactionByIdAsync(transactionId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), transactionId);

        var question = await _financeRepository.GetQuestionByTransactionAsync(transaction.Id, cancellationToken);
        if (question is null) return null;

        return new TransactionQuestionDto(
            question.Id,
            transaction.Id,
            question.QuestionType,
            question.ContextNote,
            question.MessageSentAt,
            question.MemberReply,
            question.ReplyReceivedAt,
            question.ResolutionStatus,
            question.ResolvedAt);
    }

    // ── Budget ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<BudgetDto>> GetBudgetsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);
        await EnsureModuleEnabledAsync(familyId, cancellationToken);

        var now       = DateTime.UtcNow;
        var monthYear = new DateOnly(now.Year, now.Month, 1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var budgets    = await _financeRepository.ListBudgetsAsync(familyId, monthYear, cancellationToken);
        var catSpend   = await _financeRepository.GetCategorySpendAsync(familyId, monthStart, monthEnd, cancellationToken);
        var spendLookup = catSpend.ToDictionary(c => c.Category, c => c.Total);

        var result = FinanceCategory.All.Select(cat =>
        {
            var budget = budgets.FirstOrDefault(b => b.Category == cat);
            var amount = budget?.BudgetAmount ?? 0m;
            var actual = spendLookup.GetValueOrDefault(cat, 0m);
            var remaining = amount - actual;
            decimal? utilisation = amount > 0 ? Math.Round(actual / amount * 100m, 1) : null;
            var status = utilisation switch
            {
                > 100m => "Red",
                > 80m  => "Amber",
                _      => "Green"
            };
            return new BudgetDto(cat, amount, actual, remaining, utilisation, status);
        }).ToArray();

        return result;
    }

    public async Task<BudgetDto> SetBudgetAsync(
        Guid currentUserId, Guid familyId,
        SetBudgetRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureCfoAsync(currentUserId, familyId, cancellationToken);

        var now       = DateTime.UtcNow;
        var monthYear = new DateOnly(now.Year, now.Month, 1);

        var existing = await _financeRepository.GetBudgetAsync(familyId, request.Category, monthYear, cancellationToken);
        var budget = existing ?? new Budget
        {
            FamilyId    = member.FamilyId,
            Category    = request.Category,
            MonthYear   = monthYear.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            SetByUserId = member.UserId
        };
        budget.BudgetAmount = request.BudgetAmount;
        budget.SetByUserId  = member.UserId;

        await _financeRepository.UpsertBudgetAsync(budget, cancellationToken);

        return new BudgetDto(request.Category, request.BudgetAmount, 0m, request.BudgetAmount, null, "Green");
    }

    // ── Category Breakdown ─────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<CategorySpendDto>> GetCategoryBreakdownAsync(
        Guid currentUserId, Guid familyId,
        DateTime? fromDate, DateTime? toDate,
        CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);
        await EnsureModuleEnabledAsync(familyId, cancellationToken);

        var now        = DateTime.UtcNow;
        var resolvedFrom = fromDate ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolvedTo   = toDate   ?? now;

        var cats      = await _financeRepository.GetCategorySpendAsync(familyId, resolvedFrom, resolvedTo, cancellationToken);
        var totalSpend = cats.Sum(c => c.Total);

        return cats
            .OrderByDescending(c => c.Total)
            .Select(c => new CategorySpendDto(
                c.Category,
                c.Total,
                c.Count,
                totalSpend > 0 ? Math.Round(c.Total / totalSpend * 100m, 1) : 0m,
                c.TopMerchant))
            .ToArray();
    }

    // ── Commitments ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<CommitmentDto>> ListCommitmentsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureCfoAsync(currentUserId, familyId, cancellationToken);
        await EnsureModuleEnabledAsync(familyId, cancellationToken);

        var commitments = await _financeRepository.ListCommitmentsAsync(familyId, cancellationToken);
        var allMembers  = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);

        return commitments.Select(c => MapToCommitmentDto(c, allMembers)).ToArray();
    }

    // ── Consent ────────────────────────────────────────────────────────────────

    public async Task<ConsentInviteDto> InviteConsentAsync(
        Guid currentUserId, Guid familyId,
        InviteConsentRequest request, CancellationToken cancellationToken)
    {
        var cfoMember = await EnsureCfoAsync(currentUserId, familyId, cancellationToken);

        var member = (await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken))
            .FirstOrDefault(m => m.Id == request.MemberId)
            ?? throw new NotFoundException(nameof(FamilyMember), request.MemberId);

        var existing = await _financeRepository.GetConsentByMemberAsync(request.MemberId, cancellationToken);
        var token    = GenerateSecureToken();

        var consent = existing ?? new FinanceConsent
        {
            FamilyId       = member.FamilyId,
            FamilyMemberId = member.InternalId
        };

        consent.PrivacyTier    = request.PrivacyTier;
        consent.ConsentStatus  = "Invited";
        consent.ConsentToken   = token;
        consent.InvitedAt      = DateTime.UtcNow;

        if (existing is null)
            await _financeRepository.AddConsentAsync(consent, cancellationToken);
        else
            await _financeRepository.UpdateConsentAsync(consent, cancellationToken);

        var cfoName  = cfoMember.DisplayName ?? "Your family";
        var tierDesc = request.PrivacyTier switch
        {
            PrivacyTier.FullVisibility => "Full visibility: all transactions including merchant names.",
            PrivacyTier.CategoryOnly   => "Category-only: amounts and categories. Merchant names are private.",
            PrivacyTier.AggregateOnly  => "Aggregate-only: monthly total only. No individual transactions.",
            _                          => string.Empty
        };

        return new ConsentInviteDto(
            consent.Id, member.DisplayName ?? string.Empty, cfoName,
            request.PrivacyTier, CurrentConsentVersion, tierDesc);
    }

    public async Task AcceptConsentAsync(
        Guid familyId, AcceptFinanceConsentRequest request,
        string ipAddress, CancellationToken cancellationToken)
    {
        var consent = await _financeRepository.GetConsentByTokenAsync(request.ConsentToken, cancellationToken)
            ?? throw new NotFoundException("Consent invite not found or already used.");

        if (consent.Family.Id != familyId)
            throw new ForbiddenAccessException();

        if (consent.ConsentStatus is "Accepted" or "OptedOut")
            throw new ConflictException("Consent token is no longer valid.");

        consent.ConsentStatus    = "Accepted";
        consent.ConsentGivenAt   = DateTime.UtcNow;
        consent.ConsentVersion   = request.ConsentVersion;
        consent.ConsentIpAddress = ipAddress;
        consent.ConsentToken     = null;

        await _financeRepository.UpdateConsentAsync(consent, cancellationToken);
    }

    public async Task DeclineConsentAsync(
        Guid familyId, string consentToken, CancellationToken cancellationToken)
    {
        var consent = await _financeRepository.GetConsentByTokenAsync(consentToken, cancellationToken)
            ?? throw new NotFoundException("Consent invite not found.");

        if (consent.Family.Id != familyId)
            throw new ForbiddenAccessException();

        consent.ConsentStatus = "Declined";
        consent.ConsentToken  = null;
        await _financeRepository.UpdateConsentAsync(consent, cancellationToken);
    }

    public async Task RevokeConsentAsync(
        Guid currentUserId, Guid familyId, Guid memberId,
        CancellationToken cancellationToken)
    {
        // Either the CFO or the member themselves may revoke
        var currentMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        var isCfo = await IsCfoAsync(currentUserId, familyId, cancellationToken);
        if (!isCfo && currentMember.Id != memberId)
            throw new ForbiddenAccessException("Only the CFO or the member themselves can revoke consent.");

        var consent = await _financeRepository.GetConsentByMemberAsync(memberId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceConsent), memberId);

        consent.ConsentStatus = "OptedOut";
        consent.OptedOutAt    = DateTime.UtcNow;
        await _financeRepository.UpdateConsentAsync(consent, cancellationToken);

        // Soft-delete all transaction data immediately on opt-out.
        // RawSmsText set to null via PurgeOptOutTransactionsAsync (sensitive — no grace period).
        await _financeRepository.PurgeOptOutTransactionsAsync(memberId, cancellationToken);
    }

    // ── Settings ───────────────────────────────────────────────────────────────

    public async Task<FinanceSettingsDto> GetSettingsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role is not (UserRole.FamilyAdmin or UserRole.Parent))
            throw new ForbiddenAccessException("FamilyAdmin or CFO required.");

        var settings   = await _financeRepository.GetSettingsAsync(familyId, cancellationToken);
        var consents   = await _financeRepository.ListConsentsByFamilyAsync(familyId, cancellationToken);
        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);

        Guid? cfoMemberId = settings?.CfoFamilyMember?.Id
            ?? (settings?.CfoFamilyMemberId.HasValue == true
                ? allMembers.FirstOrDefault(m => m.InternalId == settings.CfoFamilyMemberId.Value)?.Id
                : null);
        string? cfoName   = cfoMemberId.HasValue
            ? allMembers.FirstOrDefault(m => m.Id == cfoMemberId.Value)?.DisplayName
            : null;

        var memberSettings = consents.Select(c =>
        {
            var m = allMembers.FirstOrDefault(x => x.InternalId == c.FamilyMemberId);
            return new MemberFinanceSettingDto(
                m?.Id ?? c.FamilyMember?.Id ?? Guid.Empty, m?.DisplayName ?? string.Empty,
                c.PrivacyTier, c.ConsentStatus, c.ConsentGivenAt, c.OptedOutAt);
        }).ToArray();

        return new FinanceSettingsDto(
            cfoMemberId, cfoName,
            settings?.IsModuleEnabled ?? false,
            memberSettings);
    }

    public async Task<FinanceSettingsDto> UpdateSettingsAsync(
        Guid currentUserId, Guid familyId,
        UpdateFinanceSettingsRequest request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException("Only FamilyAdmin can update finance settings.");

        var settings = await _financeRepository.GetSettingsAsync(familyId, cancellationToken)
            ?? new FinanceSetting { FamilyId = member.FamilyId };

        if (request.CfoMemberId.HasValue)
        {
            var cfoMember = await _memberRepository.GetByIdAsync(request.CfoMemberId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(FamilyMember), request.CfoMemberId.Value);
            settings.CfoFamilyMemberId = cfoMember.InternalId;
        }

        if (!settings.IsModuleEnabled && request.CfoMemberId.HasValue)
        {
            settings.IsModuleEnabled = true;
            settings.EnabledAt = DateTime.UtcNow;
        }

        await _financeRepository.UpsertSettingsAsync(settings, cancellationToken);

        // Apply tier changes — tier upgrades (more privacy) immediate; downgrades require re-consent
        if (request.MemberTierChanges is { Count: > 0 })
        {
            var consents = await _financeRepository.ListConsentsByFamilyAsync(familyId, cancellationToken);
            foreach (var change in request.MemberTierChanges)
            {
                if (!PrivacyTier.IsValid(change.PrivacyTier))
                    throw new ValidationException(
                        new Dictionary<string, string[]> { ["PrivacyTier"] = ["PrivacyTier must be 1, 2, or 3."] });

                var consent = consents.FirstOrDefault(c => c.FamilyMember?.Id == change.MemberId);
                if (consent is null) continue;

                if (change.PrivacyTier < consent.PrivacyTier)
                {
                    // Downgrade (less privacy) requires member re-consent — re-invite
                    consent.ConsentStatus = "Invited";
                    consent.ConsentToken  = GenerateSecureToken();
                    consent.InvitedAt     = DateTime.UtcNow;
                    await _financeRepository.UpdateConsentAsync(consent, cancellationToken);
                }
                else
                {
                    // Upgrade (more privacy) takes effect immediately
                    consent.PrivacyTier = change.PrivacyTier;
                    await _financeRepository.UpdateConsentAsync(consent, cancellationToken);
                }
            }
        }

        return await GetSettingsAsync(currentUserId, familyId, cancellationToken);
    }

    // ── Privacy filter — enforced on every data read ───────────────────────────

    private static TransactionDto? ApplyPrivacyFilter(
        Transaction t, int tier, IReadOnlyCollection<FamilyMember> allMembers)
    {
        // Tier 3: aggregate only — no line items returned to dashboard feed
        if (tier == PrivacyTier.AggregateOnly)
            return null;

        var member = allMembers.FirstOrDefault(m => m.InternalId == t.FamilyMemberId);
        var memberName = member?.DisplayName ?? t.FamilyMember?.DisplayName ?? string.Empty;

        // Tier 2: hash merchant name; blur personal categories unless >₹5,000
        bool isBlurred = tier == PrivacyTier.CategoryOnly
            && FinanceCategory.Tier2Blurred.Contains(t.Category)
            && t.Amount < PrivacyTier.Tier2LargeTransactionThreshold;

        if (isBlurred) return null;

        string? merchantName = tier == PrivacyTier.FullVisibility ? t.MerchantName : null;

        return new TransactionDto(
            t.Id, member?.Id ?? t.FamilyMember?.Id ?? Guid.Empty, memberName,
            merchantName, t.MerchantNameHash,
            t.Amount, t.TransactionType, t.Category,
            tier == PrivacyTier.CategoryOnly && FinanceCategory.Tier2Blurred.Contains(t.Category),
            tier, t.QuestionStatus, t.ParsedAt);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<FamilyMember> EnsureCfoAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, userId, cancellationToken)
            ?? throw new ForbiddenAccessException("Only the designated Family CFO can access finance data.");

        if (!await IsCfoAsync(userId, familyId, cancellationToken))
            throw new ForbiddenAccessException("Only the designated Family CFO can access finance data.");

        return member;
    }

    private async Task<bool> IsCfoAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var settings = await _financeRepository.GetSettingsAsync(familyId, cancellationToken);
        if (settings is null) return false;
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, userId, cancellationToken);
        return member is not null && member.InternalId == settings.CfoFamilyMemberId;
    }

    private async Task EnsureModuleEnabledAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var settings = await _financeRepository.GetSettingsAsync(familyId, cancellationToken);
        if (settings is null || !settings.IsModuleEnabled)
            throw new ForbiddenAccessException("Finance module is not enabled for this family.");
    }

    private async Task EnsureConsentAsync(Guid familyId, Guid memberId, CancellationToken cancellationToken)
    {
        var consent = await _financeRepository.GetConsentByMemberAsync(memberId, cancellationToken);
        if (consent is null || consent.ConsentStatus != "Accepted")
            throw new ForbiddenAccessException(
                "This member has not given consent for finance data sharing.");
    }

    private static int GetConsentTier(IReadOnlyCollection<FinanceConsent> consents, Guid memberId)
    {
        var consent = consents.FirstOrDefault(c => c.FamilyMember?.Id == memberId);
        return consent?.PrivacyTier ?? PrivacyTier.AggregateOnly;
    }

    private static int GetConsentTierByInternalMemberId(IReadOnlyCollection<FinanceConsent> consents, long memberId)
    {
        var consent = consents.FirstOrDefault(c => c.FamilyMemberId == memberId);
        return consent?.PrivacyTier ?? PrivacyTier.AggregateOnly;
    }

    private async Task<(decimal? Today, decimal? Month, bool IsAboveThreshold)> GetMemberSpendForCardAsync(
        Guid familyId, Guid memberId, int tier,
        DateTime monthStart, DateTime dayStart,
        CancellationToken cancellationToken)
    {
        if (tier == PrivacyTier.AggregateOnly)
        {
            var (items, _) = await _financeRepository.ListTransactionsAsync(
                familyId, memberId, null, monthStart, monthStart.AddMonths(1), 1, int.MaxValue, cancellationToken);
            var monthTotal = items.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
            return (null, monthTotal, false);
        }

        var (allItems, _) = await _financeRepository.ListTransactionsAsync(
            familyId, memberId, null, monthStart, monthStart.AddMonths(1), 1, int.MaxValue, cancellationToken);

        var monthSpend = allItems.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
        var todaySpend = allItems
            .Where(t => t.TransactionType == "Debit" && t.ParsedAt >= dayStart)
            .Sum(t => t.Amount);
        var isAbove = tier == PrivacyTier.CategoryOnly
            && allItems.Any(t => t.Amount >= PrivacyTier.Tier2LargeTransactionThreshold);

        return (todaySpend, monthSpend, isAbove);
    }

    private static IReadOnlyCollection<FinanceAlertDto> BuildAlerts(
        decimal totalSpend, decimal totalIncome, IReadOnlyCollection<Commitment> missed)
    {
        var alerts = new List<FinanceAlertDto>();
        if (totalIncome > 0 && totalSpend / totalIncome > 1.0m)
            alerts.Add(new FinanceAlertDto("Overspend", "Monthly spend exceeds income.", "Critical", null, null));
        else if (totalIncome > 0 && totalSpend / totalIncome > 0.9m)
            alerts.Add(new FinanceAlertDto("NearLimit", "Spend is above 90% of income.", "Warning", null, null));

        foreach (var c in missed)
            alerts.Add(new FinanceAlertDto("CommitmentMissed",
                $"{c.CommitmentName} payment appears missed.", "Critical", null, c.Id));

        return alerts;
    }

    private static CommitmentDto MapToCommitmentDto(
        Commitment c, IReadOnlyCollection<FamilyMember> members) =>
        new(c.Id,
            members.FirstOrDefault(m => m.InternalId == c.FamilyMemberId)?.Id ?? c.FamilyMember?.Id ?? Guid.Empty,
            members.FirstOrDefault(m => m.InternalId == c.FamilyMemberId)?.DisplayName ?? c.FamilyMember?.DisplayName ?? string.Empty,
            c.CommitmentName, c.CommitmentType, c.Amount, c.FrequencyType,
            DateOnly.FromDateTime(c.NextDueDate), c.Status, c.IsConfirmed);

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
