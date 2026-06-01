using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class CoinService : ICoinService
{
    private const string EarnedTransactionType = "Earned";
    private const string SpentTransactionType = "Spent";
    private const string DeductedTransactionType = "Deducted";
    private const string ManualDeductionReferenceType = "ManualDeduction";
    private const string RewardRedemptionReferenceType = "RewardRedemption";

    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IRewardRedemptionRepository _rewardRedemptionRepository;
    private readonly ITaskCompletionRepository _taskCompletionRepository;
    private readonly ITaskItemRepository _taskItemRepository;

    public CoinService(
        IChildProfileRepository childProfileRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IFamilyMemberRepository familyMemberRepository,
        IRewardRedemptionRepository rewardRedemptionRepository,
        ITaskCompletionRepository taskCompletionRepository,
        ITaskItemRepository taskItemRepository)
    {
        _childProfileRepository = childProfileRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _familyMemberRepository = familyMemberRepository;
        _rewardRedemptionRepository = rewardRedemptionRepository;
        _taskCompletionRepository = taskCompletionRepository;
        _taskItemRepository = taskItemRepository;
    }

    public async Task<CoinTransactionDto> EarnCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childProfileId,
        int amount,
        string referenceType,
        Guid? referenceId,
        string? note,
        string? pillarTag,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!new[] { UserRole.Parent, UserRole.FamilyAdmin }.Contains(member.Role))
        {
            throw new ForbiddenAccessException("User role is not allowed for this coin operation.");
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);
        childProfile.CoinBalance += amount;
        childProfile.TotalCoinsEarned += amount;
        childProfile.LevelCode = CalculateLevelCode(childProfile.TotalCoinsEarned);
        UpdatePillarScore(childProfile, pillarTag);
        await ApplyStreakProgressAsync(childProfile, familyId, cancellationToken);

        var coinTransaction = new CoinTransaction
        {
            ChildProfileId = childProfile.InternalId,
            FamilyId = childProfile.FamilyId,
            TransactionType = EarnedTransactionType,
            Amount = amount,
            BalanceAfter = childProfile.CoinBalance,
            ReferenceType = referenceType,
            ReferenceId = null, // ReferenceId is long? in entity; Guid? from caller — skip
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedByUserId = member.UserId
        };

        await ApplyMutationAsync(childProfile, coinTransaction, cancellationToken);

        return ToDto(coinTransaction);
    }

    public async Task<CoinTransactionDto> DeductCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childProfileId,
        DeductCoinsRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException("User role is not allowed for this coin operation.");
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (string.IsNullOrWhiteSpace(note) || note.Length < 5)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Note"] = new[] { "Reason is required for coin deduction." }
                });
        }

        if (request.Amount > childProfile.CoinBalance)
        {
            throw new ConflictException("Child coin balance is insufficient.");
        }

        childProfile.CoinBalance -= request.Amount;

        var coinTransaction = new CoinTransaction
        {
            ChildProfileId = childProfile.InternalId,
            FamilyId = childProfile.FamilyId,
            TransactionType = DeductedTransactionType,
            Amount = -request.Amount,
            BalanceAfter = childProfile.CoinBalance,
            ReferenceType = ManualDeductionReferenceType,
            ReferenceId = null,
            Note = note,
            CreatedByUserId = member.UserId
        };

        await ApplyMutationAsync(childProfile, coinTransaction, cancellationToken);

        return ToDto(coinTransaction);
    }

    public async Task<CoinTransactionDto> SpendCoinsForRewardRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Reward reward,
        RewardRedemption rewardRedemption,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!new[] { UserRole.Parent, UserRole.FamilyAdmin }.Contains(member.Role))
        {
            throw new ForbiddenAccessException("User role is not allowed for this coin operation.");
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(rewardRedemption.ChildProfile?.Id ?? Guid.Empty, familyId, cancellationToken);

        if (rewardRedemption.CoinsSpent > childProfile.CoinBalance)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CoinBalance"] = new[] { "Child coin balance is insufficient for this redemption." }
                },
                422);
        }

        childProfile.CoinBalance -= rewardRedemption.CoinsSpent;
        reward.TimesRedeemedTotal++;

        var coinTransaction = new CoinTransaction
        {
            ChildProfileId = childProfile.InternalId,
            FamilyId = childProfile.FamilyId,
            TransactionType = SpentTransactionType,
            Amount = -rewardRedemption.CoinsSpent,
            BalanceAfter = childProfile.CoinBalance,
            ReferenceType = RewardRedemptionReferenceType,
            ReferenceId = null, // ReferenceId is long? in entity; Guid from redemption — skip
            Note = reward.RewardName,
            CreatedByUserId = member.UserId
        };

        try
        {
            await _rewardRedemptionRepository.ApplyApprovalAsync(
                rewardRedemption,
                reward,
                childProfile,
                coinTransaction,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Concurrent redemption approval detected. Please retry the operation.");
        }

        return ToDto(coinTransaction);
    }

    public async Task<PaginatedList<CoinTransactionDto>> GetHistoryAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childProfileId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.Child)
        {
            throw new ForbiddenAccessException("Parent or Child role is required.");
        }

        if (member.Role == UserRole.Child && currentChildProfileId != childProfileId)
        {
            throw new ForbiddenAccessException("Child can view only their own coin history.");
        }

        await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);
        var transactions = await _coinTransactionRepository.ListByChildAsync(familyId, childProfileId, cancellationToken);

        return PaginatedList<CoinTransactionDto>.Create(
            transactions.Select(ToDto),
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize <= 0 ? 20 : pageSize);
    }

    public async Task<bool> UseStreakFreezeAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child || currentChildProfileId != childProfileId)
        {
            throw new ForbiddenAccessException("Child can use streak freeze only for their own profile.");
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);

        if (childProfile.StreakFreezesAvailable <= 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["StreakFreezesAvailable"] = new[] { "No streak freezes available." }
                },
                422);
        }

        childProfile.StreakFreezesAvailable--;
        await ApplyMutationAsync(childProfile, null, cancellationToken);

        return true;
    }

    private async Task ApplyMutationAsync(
        ChildProfile childProfile,
        CoinTransaction? coinTransaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await _coinTransactionRepository.ApplyAsync(childProfile, coinTransaction, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Concurrent coin update detected. Please retry the operation.");
        }
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childProfileId);
        }

        return childProfile;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task EnsureFamilyRoleAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken,
        params UserRole[] allowedRoles)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!allowedRoles.Contains(member.Role))
        {
            throw new ForbiddenAccessException("User role is not allowed for this coin operation.");
        }
    }

    private static int CalculateLevelCode(int totalCoinsEarned)
    {
        return totalCoinsEarned switch
        {
            >= 5000 => 5,
            >= 3000 => 4,
            >= 1500 => 3,
            >= 500 => 2,
            _ => 1
        };
    }

    private static void UpdatePillarScore(ChildProfile childProfile, string? pillarTag)
    {
        switch (pillarTag)
        {
            case "Study":
                childProfile.StudyScore = Math.Min(20, childProfile.StudyScore + 1);
                break;
            case "Cleanliness":
                childProfile.CleanlinessScore = Math.Min(20, childProfile.CleanlinessScore + 1);
                break;
            case "Discipline":
                childProfile.DisciplineScore = Math.Min(20, childProfile.DisciplineScore + 1);
                break;
            case "ScreenControl":
                childProfile.ScreenControlScore = Math.Min(20, childProfile.ScreenControlScore + 1);
                break;
            case "Responsibility":
                childProfile.ResponsibilityScore = Math.Min(20, childProfile.ResponsibilityScore + 1);
                break;
            default:
                return;
        }

        childProfile.ScoreUpdatedAt = DateTime.UtcNow;
    }

    private async Task ApplyStreakProgressAsync(
        ChildProfile childProfile,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);
        var totalTasks = taskItems.Count(taskItem =>
            (!taskItem.ChildProfileId.HasValue || taskItem.ChildProfileId == childProfile.InternalId)
            && IsActiveForDate(taskItem, utcToday));

        if (totalTasks <= 0)
        {
            return;
        }

        var approvals = await _taskCompletionRepository.ListByFamilyAsync(familyId, childProfile.Id, utcToday, cancellationToken);
        var approvedCount = approvals.Count(taskCompletion => taskCompletion.Status == TaskStatus.Approved);
        var approvalRatio = approvedCount / (double)totalTasks;

        if (approvalRatio >= 0.5d)
        {
            childProfile.CurrentStreakDays++;
            childProfile.BestStreakDays = Math.Max(childProfile.BestStreakDays, childProfile.CurrentStreakDays);

            if (childProfile.CurrentStreakDays % 10 == 0 && childProfile.StreakFreezesAvailable < 2)
            {
                childProfile.StreakFreezesAvailable++;
            }
        }
    }

    private static bool IsActiveForDate(TaskItem taskItem, DateOnly targetDate)
    {
        var activeFrom = DateOnly.FromDateTime(taskItem.ActiveFromDate);
        var activeTo = taskItem.ActiveToDate.HasValue ? DateOnly.FromDateTime(taskItem.ActiveToDate.Value) : (DateOnly?)null;

        if (targetDate < activeFrom)
        {
            return false;
        }

        if (activeTo.HasValue && targetDate > activeTo.Value)
        {
            return false;
        }

        if (!taskItem.IsRecurring)
        {
            return targetDate == activeFrom;
        }

        var recurringDays = System.Text.Json.JsonSerializer.Deserialize<int[]>(taskItem.RecurringDays) ?? Array.Empty<int>();
        var dayOfWeek = targetDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)targetDate.DayOfWeek;

        return recurringDays.Contains(dayOfWeek);
    }

    private static CoinTransactionDto ToDto(CoinTransaction coinTransaction)
    {
        return new CoinTransactionDto(
            coinTransaction.Id,
            coinTransaction.ChildProfile?.Id ?? Guid.Empty,
            coinTransaction.Family?.Id ?? Guid.Empty,
            coinTransaction.TransactionType,
            coinTransaction.Amount,
            coinTransaction.BalanceAfter,
            coinTransaction.ReferenceType,
            null, // ReferenceId is long? in entity; Guid? in DTO — not mappable directly
            coinTransaction.Note,
            coinTransaction.CreatedByUser?.Id ?? Guid.Empty,
            coinTransaction.DateCreated);
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }
}
