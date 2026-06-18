using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class RewardService : IRewardService
{
    private readonly ICoinService _coinService;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IRewardRepository _rewardRepository;
    private readonly IRewardRedemptionRepository _rewardRedemptionRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public RewardService(
        IRewardRepository rewardRepository,
        IRewardRedemptionRepository rewardRedemptionRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        IPushNotificationService pushNotificationService,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _rewardRepository = rewardRepository;
        _rewardRedemptionRepository = rewardRedemptionRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _pushNotificationService = pushNotificationService;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<IReadOnlyCollection<RewardDto>> ListSystemRewardsAsync(
        Guid currentUserId,
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserId, currentUserRole, cancellationToken);

        var rewards = await _rewardRepository.ListSystemRewardsAsync(cancellationToken);
        var response = rewards.Select(ToDto).ToArray();
        LogApiCall(nameof(ListSystemRewardsAsync), new { currentUserId, currentUserRole }, new { Count = response.Length });
        return response;
    }

    public async Task<RewardDto> CreateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserId, currentUserRole, cancellationToken);

        var reward = new Reward
        {
            FamilyId = null,
            MasterRewardId = null,
            RewardName = request.RewardName.Trim(),
            Description = NormalizeOptional(request.Description),
            IconCode = NormalizeOptional(request.IconCode),
            Category = NormalizeCategory(request.Category),
            CoinCost = request.CoinCost,
            IsSystem = true,
            IsEnabled = true
        };

        await _rewardRepository.AddAsync(reward, cancellationToken);

        var response = ToDto(reward);
        LogApiCall(nameof(CreateSystemRewardAsync), new { currentUserId, currentUserRole, request.RewardName, request.CoinCost }, new { response.RewardId });
        return response;
    }

    public async Task<RewardDto> UpdateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserId, currentUserRole, cancellationToken);

        var reward = await GetRewardOrThrowAsync(rewardId, null, cancellationToken);

        if (!reward.IsSystem || reward.FamilyId.HasValue)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        reward.RewardName = request.RewardName.Trim();
        reward.CoinCost = request.CoinCost;
        reward.IsEnabled = request.IsEnabled;

        await _rewardRepository.UpdateAsync(reward, cancellationToken);

        var response = ToDto(reward);
        LogApiCall(nameof(UpdateSystemRewardAsync), new { currentUserId, currentUserRole, rewardId }, new { response.RewardId, response.IsEnabled });
        return response;
    }

    public async Task<IReadOnlyCollection<RewardDto>> ListFamilyRewardsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var familyRewards = await _rewardRepository.ListFamilyRewardsAsync(familyId, cancellationToken);

        if (member.Role == UserRole.Child)
        {
            var childResponse = familyRewards
                .Where(reward => reward.IsEnabled)
                .OrderBy(reward => reward.Category)
                .ThenBy(reward => reward.CoinCost)
                .ThenBy(reward => reward.RewardName)
                .Select(ToDto)
                .ToArray();
            LogApiCall(nameof(ListFamilyRewardsAsync), new { currentUserId, familyId, member.Role }, new { Count = childResponse.Length });
            return childResponse;
        }

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var familyRewardMasterIds = familyRewards
            .Where(reward => reward.MasterRewardId.HasValue)
            .Select(reward => reward.MasterReward?.Id)
            .Where(masterRewardId => masterRewardId.HasValue)
            .Select(masterRewardId => masterRewardId!.Value)
            .ToHashSet();
        var systemRewards = await _rewardRepository.ListSystemRewardsAsync(cancellationToken);
        var availableSystemTemplates = systemRewards
            .Where(reward => !familyRewardMasterIds.Contains(reward.Id))
            .Select(reward => new RewardDto(
                reward.Id,
                null,
                null,
                reward.RewardName,
                reward.Description,
                reward.IconCode,
                reward.Category,
                reward.CoinCost,
                reward.IsSystem,
                false,
                reward.TimesRedeemedTotal));

        var response = familyRewards
            .Select(ToDto)
            .Concat(availableSystemTemplates)
            .OrderBy(reward => reward.Category)
            .ThenBy(reward => reward.CoinCost)
            .ThenBy(reward => reward.RewardName)
            .ToArray();
        LogApiCall(nameof(ListFamilyRewardsAsync), new { currentUserId, familyId, member.Role }, new { Count = response.Length });
        return response;
    }

    public async Task<RewardDto> CreateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var reward = new Reward
        {
            FamilyId = member.FamilyId,
            MasterRewardId = null,
            RewardName = request.RewardName.Trim(),
            Description = NormalizeOptional(request.Description),
            IconCode = NormalizeOptional(request.IconCode),
            Category = NormalizeCategory(request.Category),
            CoinCost = request.CoinCost,
            IsSystem = false,
            IsEnabled = true
        };

        await _rewardRepository.AddAsync(reward, cancellationToken);

        var response = ToDto(reward);
        LogApiCall(nameof(CreateFamilyRewardAsync), new { currentUserId, familyId, request.RewardName, request.CoinCost }, new { response.RewardId });
        return response;
    }

    public async Task<RewardDto> UpdateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var reward = await GetRewardOrThrowAsync(rewardId, familyId, cancellationToken);

        if (reward.IsSystem && !reward.FamilyId.HasValue)
        {
            var familyCopy = await _rewardRepository.GetFamilyCopyByMasterRewardIdAsync(familyId, reward.Id, cancellationToken);

            if (familyCopy is null)
            {
                familyCopy = new Reward
                {
                    FamilyId = member.FamilyId,
                    MasterRewardId = reward.InternalId,
                    RewardName = request.RewardName.Trim(),
                    Description = reward.Description,
                    IconCode = reward.IconCode,
                    Category = reward.Category,
                    CoinCost = request.CoinCost,
                    IsSystem = false,
                    IsEnabled = request.IsEnabled
                };

                await _rewardRepository.AddAsync(familyCopy, cancellationToken);
            }
            else
            {
                familyCopy.RewardName = request.RewardName.Trim();
                familyCopy.CoinCost = request.CoinCost;
                familyCopy.IsEnabled = request.IsEnabled;

                await _rewardRepository.UpdateAsync(familyCopy, cancellationToken);
            }

            var familyCopyResponse = ToDto(familyCopy);
            LogApiCall(nameof(UpdateFamilyRewardAsync), new { currentUserId, familyId, rewardId }, new { familyCopyResponse.RewardId, familyCopyResponse.IsEnabled });
            return familyCopyResponse;
        }

        if (reward.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        reward.RewardName = request.RewardName.Trim();
        reward.CoinCost = request.CoinCost;
        reward.IsEnabled = request.IsEnabled;

        await _rewardRepository.UpdateAsync(reward, cancellationToken);

        var response = ToDto(reward);
        LogApiCall(nameof(UpdateFamilyRewardAsync), new { currentUserId, familyId, rewardId }, new { response.RewardId, response.IsEnabled });
        return response;
    }

    public async Task<RedemptionDto> RedeemAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid rewardId,
        RedeemRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (!currentChildProfileId.HasValue || currentChildProfileId.Value != request.ChildProfileId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var reward = await GetRewardOrThrowAsync(rewardId, familyId, cancellationToken);
        var childProfile = await GetChildInFamilyOrThrowAsync(request.ChildProfileId, familyId, cancellationToken);
        var familyReward = await ResolveRewardForFamilyAsync(reward, familyId, cancellationToken);

        if (!familyReward.IsEnabled)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["RewardId"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                });
        }

        if (childProfile.CoinBalance < familyReward.CoinCost)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CoinBalance"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Insufficient_Coins, cancellationToken) }
                },
                422);
        }

        if (await _rewardRedemptionRepository.GetPendingByChildAndRewardAsync(childProfile.Id, familyReward.Id, cancellationToken) is not null)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Reward_Already_Redeemed, cancellationToken));
        }

        var redemption = new RewardRedemption
        {
            RewardId = familyReward.InternalId,
            ChildProfileId = childProfile.InternalId,
            FamilyId = childProfile.FamilyId,
            CoinsSpent = familyReward.CoinCost,
            Status = RedemptionStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        await _rewardRedemptionRepository.AddAsync(redemption, cancellationToken);
        redemption.Reward = familyReward;
        redemption.ChildProfile = childProfile;

        var response = ToRedemptionDto(redemption);
        LogApiCall(nameof(RedeemAsync), new { currentUserId, familyId, rewardId, request.ChildProfileId }, new { response.RedemptionId, response.Status });
        return response;
    }

    public async Task<IReadOnlyCollection<RedemptionDto>> ListRedemptionsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid? childId,
        RedemptionStatus? status,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);

        if (childId.HasValue)
        {
            await GetChildInFamilyOrThrowAsync(childId.Value, familyId, cancellationToken);
        }

        var redemptions = await _rewardRedemptionRepository.ListByFamilyAsync(
            familyId,
            childId,
            status,
            cancellationToken);

        var response = redemptions.Select(ToRedemptionDto).ToArray();
        LogApiCall(nameof(ListRedemptionsAsync), new { currentUserId, familyId, childId, status }, new { Count = response.Length });
        return response;
    }

    public async Task<RedemptionDto> ReviewRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid redemptionId,
        ReviewRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.ApproveReject, cancellationToken);

        var redemption = await GetRedemptionOrThrowAsync(redemptionId, familyId, cancellationToken);

        if (redemption.Status != RedemptionStatus.Pending)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken));
        }

        redemption.ReviewedByUserId = member.UserId;
        redemption.ReviewedAt = DateTime.UtcNow;
        redemption.ParentNote = NormalizeOptional(request.ParentNote);

        if (request.Status == RedemptionStatus.Approved)
        {
            redemption.Status = RedemptionStatus.Approved;
            await _coinService.SpendCoinsForRewardRedemptionAsync(
                currentUserId,
                familyId,
                redemption.Reward!,
                redemption,
                cancellationToken);

            await SendPushToChildAsync(
                redemption.ChildProfile!,
                "Reward approved",
                $"{redemption.Reward!.RewardName} was approved.",
                cancellationToken);
        }
        else
        {
            redemption.Status = RedemptionStatus.Rejected;
            await _rewardRedemptionRepository.UpdateAsync(redemption, cancellationToken);

            var body = string.IsNullOrWhiteSpace(redemption.ParentNote)
                ? $"{redemption.Reward!.RewardName} was rejected."
                : $"{redemption.Reward!.RewardName} was rejected: {redemption.ParentNote}";

            await SendPushToChildAsync(
                redemption.ChildProfile!,
                "Reward rejected",
                body,
                cancellationToken);
        }

        var response = ToRedemptionDto(redemption);
        LogApiCall(nameof(ReviewRedemptionAsync), new { currentUserId, familyId, redemptionId, request.Status }, new { response.RedemptionId, response.Status });
        return response;
    }

    private async Task<Reward> GetRewardOrThrowAsync(Guid rewardId, Guid? familyId, CancellationToken cancellationToken)
    {
        if (familyId.HasValue)
        {
            var familyInternalId = await GetFamilyInternalIdAsync(familyId.Value, cancellationToken);
            var resolvedRewardId = await _masterDataResolver.ResolveAsync(
                MasterDataCodes.Reward,
                rewardId.ToString(),
                familyInternalId,
                cancellationToken);

            if (!resolvedRewardId.HasValue)
            {
                resolvedRewardId = await _masterDataResolver.ResolveAsync(
                    MasterDataCodes.Reward,
                    rewardId.ToString(),
                    cancellationToken: cancellationToken);
            }

            if (!resolvedRewardId.HasValue)
            {
                throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
            }
        }

        return await _rewardRepository.GetByIdAsync(rewardId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }

    private async Task<RewardRedemption> GetRedemptionOrThrowAsync(Guid redemptionId, Guid familyId, CancellationToken cancellationToken)
    {
        var redemption = await _rewardRedemptionRepository.GetByIdAsync(redemptionId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (redemption.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return redemption;
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

    private async Task<FamilyMember> EnsureFamilyManagerAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
    }

    private static string NormalizeCategory(string category)
    {
        return RewardCatalog.NormalizeCategory(category)
            ?? throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Category"] = new[]
                    {
                        $"Category must be one of: {string.Join(", ", RewardCatalog.AllowedCategories)}."
                    }
                });
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<Reward> ResolveRewardForFamilyAsync(Reward redemptionReward, Guid familyId, CancellationToken cancellationToken)
    {
        if (redemptionReward.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return redemptionReward;
    }

    private static RewardDto ToDto(Reward reward)
    {
        return new RewardDto(
            reward.Id,
            reward.Family?.Id,
            reward.MasterReward?.Id,
            reward.RewardName,
            reward.Description,
            reward.IconCode,
            reward.Category,
            reward.CoinCost,
            reward.IsSystem,
            reward.IsEnabled,
            reward.TimesRedeemedTotal);
    }

    private static RedemptionDto ToRedemptionDto(RewardRedemption rewardRedemption)
    {
        var childName = rewardRedemption.ChildProfile?.FamilyMember?.User?.FullName
            ?? rewardRedemption.ChildProfile?.User?.FullName
            ?? string.Empty;

        return new RedemptionDto(
            rewardRedemption.Id,
            rewardRedemption.Reward?.Id ?? Guid.Empty,
            rewardRedemption.ChildProfile?.Id ?? Guid.Empty,
            rewardRedemption.Family?.Id ?? Guid.Empty,
            rewardRedemption.CoinsSpent,
            rewardRedemption.Status,
            rewardRedemption.RequestedAt,
            rewardRedemption.ReviewedByUser?.Id,
            rewardRedemption.ReviewedAt,
            rewardRedemption.ParentNote,
            rewardRedemption.Reward?.RewardName ?? string.Empty,
            childName);
    }

    private async Task SendPushToChildAsync(
        ChildProfile childProfile,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var fcmToken = childProfile.User?.FcmToken
            ?? childProfile.FamilyMember?.User?.FcmToken;

        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            return;
        }

        await _pushNotificationService.SendPushAsync(fcmToken, title, body, cancellationToken);
    }

    private async Task EnsureSuperAdminAsync(Guid currentUserId, string? currentUserRole, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        if (!string.Equals(currentUserRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
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

public static class RewardCatalog
{
    public const string ScreenTime = "ScreenTime";
    public const string FoodTreat = "FoodTreat";
    public const string Outing = "Outing";
    public const string Purchase = "Purchase";
    public const string FamilyActivity = "FamilyActivity";

    public static readonly string[] AllowedCategories =
    {
        ScreenTime,
        FoodTreat,
        Outing,
        Purchase,
        FamilyActivity
    };

    public static string? NormalizeCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return AllowedCategories.FirstOrDefault(category =>
            string.Equals(category, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
