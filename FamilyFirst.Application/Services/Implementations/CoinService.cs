using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public CoinService(
        IChildProfileRepository childProfileRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IFamilyMemberRepository familyMemberRepository,
        IRewardRedemptionRepository rewardRedemptionRepository,
        ITaskCompletionRepository taskCompletionRepository,
        ITaskItemRepository taskItemRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _childProfileRepository = childProfileRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _familyMemberRepository = familyMemberRepository;
        _rewardRedemptionRepository = rewardRedemptionRepository;
        _taskCompletionRepository = taskCompletionRepository;
        _taskItemRepository = taskItemRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
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
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        if (!new[] { UserRole.Parent, UserRole.FamilyAdmin }.Contains(member.Role))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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
            ReferenceId = null,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedByUserId = member.UserId
        };

        await ApplyMutationAsync(childProfile, coinTransaction, cancellationToken);

        var response = ToDto(coinTransaction);
        LogApiCall(nameof(EarnCoinsAsync), new { currentUserId, familyId, childProfileId, amount, referenceType, referenceId }, new { response.TransactionId, response.BalanceAfter });
        return response;
    }

    public async Task<CoinTransactionDto> DeductCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childProfileId,
        DeductCoinsRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (string.IsNullOrWhiteSpace(note) || note.Length < 5)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Note"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                });
        }

        if (request.Amount > childProfile.CoinBalance)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CoinBalance"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Insufficient_Coins, cancellationToken) }
                },
                422);
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

        var response = ToDto(coinTransaction);
        LogApiCall(nameof(DeductCoinsAsync), new { currentUserId, familyId, childProfileId, request.Amount }, new { response.TransactionId, response.BalanceAfter });
        return response;
    }

    public async Task<CoinTransactionDto> SpendCoinsForRewardRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Reward reward,
        RewardRedemption rewardRedemption,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.ApproveReject, cancellationToken);

        if (!new[] { UserRole.Parent, UserRole.FamilyAdmin }.Contains(member.Role))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(rewardRedemption.ChildProfile?.Id ?? Guid.Empty, familyId, cancellationToken);

        if (rewardRedemption.CoinsSpent > childProfile.CoinBalance)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CoinBalance"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Insufficient_Coins, cancellationToken) }
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
            ReferenceId = null,
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

        var response = ToDto(coinTransaction);
        LogApiCall(nameof(SpendCoinsForRewardRedemptionAsync), new { currentUserId, familyId, rewardId = reward.Id, redemptionId = rewardRedemption.Id }, new { response.TransactionId, response.BalanceAfter });
        return response;
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (member.Role == UserRole.Child && currentChildProfileId != childProfileId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);
        var transactions = await _coinTransactionRepository.ListByChildAsync(familyId, childProfileId, cancellationToken);

        var response = PaginatedList<CoinTransactionDto>.Create(
            transactions.Select(ToDto),
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize <= 0 ? 20 : pageSize);
        LogApiCall(nameof(GetHistoryAsync), new { currentUserId, familyId, childProfileId, pageNumber, pageSize }, new { response.TotalCount });
        return response;
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(childProfileId, familyId, cancellationToken);

        if (childProfile.StreakFreezesAvailable <= 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["StreakFreezesAvailable"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                },
                422);
        }

        childProfile.StreakFreezesAvailable--;
        await ApplyMutationAsync(childProfile, null, cancellationToken);

        LogApiCall(nameof(UseStreakFreezeAsync), new { currentUserId, familyId, childProfileId }, new { Success = true, childProfile.StreakFreezesAvailable });
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
        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile,
            childProfileId.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedChildId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return childProfile;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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

        var recurringDays = JsonSerializer.Deserialize<int[]>(taskItem.RecurringDays) ?? Array.Empty<int>();
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
            null,
            coinTransaction.Note,
            coinTransaction.CreatedByUser?.Id ?? Guid.Empty,
            coinTransaction.DateCreated);
    }

    private async Task<long> GetFamilyInternalIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return resolvedFamilyId.Value;
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Rewards,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await _errorCodeService.GetMessageAsync(
            FamilyFirstErrorCode.Invalid_MasterData,
            cancellationToken: cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(MasterDataCodes)] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }
}
