using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class RewardService : IRewardService
{
    private readonly ICoinService _coinService;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IRewardRepository _rewardRepository;
    private readonly IRewardRedemptionRepository _rewardRedemptionRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IChildProfileRepository _childProfileRepository;

    public RewardService(
        IRewardRepository rewardRepository,
        IRewardRedemptionRepository rewardRedemptionRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        IPushNotificationService pushNotificationService)
    {
        _rewardRepository = rewardRepository;
        _rewardRedemptionRepository = rewardRedemptionRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<IReadOnlyCollection<RewardDto>> ListSystemRewardsAsync(
        Guid currentUserId,
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserId, currentUserRole);

        var rewards = await _rewardRepository.ListSystemRewardsAsync(cancellationToken);

        return rewards.Select(ToDto).ToArray();
    }

    public async Task<RewardDto> CreateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserId, currentUserRole);

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

        return ToDto(reward);
    }

    public async Task<RewardDto> UpdateSystemRewardAsync(
        Guid currentUserId,
        string? currentUserRole,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserId, currentUserRole);

        var reward = await GetRewardOrThrowAsync(rewardId, cancellationToken);

        if (!reward.IsSystem || reward.FamilyId.HasValue)
        {
            throw new NotFoundException(nameof(Reward), rewardId);
        }

        reward.RewardName = request.RewardName.Trim();
        reward.CoinCost = request.CoinCost;
        reward.IsEnabled = request.IsEnabled;

        await _rewardRepository.UpdateAsync(reward, cancellationToken);

        return ToDto(reward);
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
            return familyRewards
                .Where(reward => reward.IsEnabled)
                .OrderBy(reward => reward.Category)
                .ThenBy(reward => reward.CoinCost)
                .ThenBy(reward => reward.RewardName)
                .Select(ToDto)
                .ToArray();
        }

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Parent, FamilyAdmin, or Child role is required.");
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

        return familyRewards
            .Select(ToDto)
            .Concat(availableSystemTemplates)
            .OrderBy(reward => reward.Category)
            .ThenBy(reward => reward.CoinCost)
            .ThenBy(reward => reward.RewardName)
            .ToArray();
    }

    public async Task<RewardDto> CreateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        CreateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);

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

        return ToDto(reward);
    }

    public async Task<RewardDto> UpdateFamilyRewardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid rewardId,
        UpdateRewardRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);

        var reward = await GetRewardOrThrowAsync(rewardId, cancellationToken);

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

            return ToDto(familyCopy);
        }

        if (reward.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(Reward), rewardId);
        }

        reward.RewardName = request.RewardName.Trim();
        reward.CoinCost = request.CoinCost;
        reward.IsEnabled = request.IsEnabled;

        await _rewardRepository.UpdateAsync(reward, cancellationToken);

        return ToDto(reward);
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
            throw new ForbiddenAccessException("Child role is required.");
        }

        if (!currentChildProfileId.HasValue || currentChildProfileId.Value != request.ChildProfileId)
        {
            throw new ForbiddenAccessException("Child can redeem rewards only for their own profile.");
        }

        var reward = await GetRewardOrThrowAsync(rewardId, cancellationToken);
        var childProfile = await GetChildInFamilyOrThrowAsync(request.ChildProfileId, familyId, cancellationToken);
        var familyReward = ResolveRewardForFamily(redemptionReward: reward, familyId);

        if (!familyReward.IsEnabled)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["RewardId"] = new[] { "Reward is not enabled for this family." }
                });
        }

        if (childProfile.CoinBalance < familyReward.CoinCost)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CoinBalance"] = new[] { "Child coin balance is insufficient for this redemption." }
                },
                422);
        }

        if (await _rewardRedemptionRepository.GetPendingByChildAndRewardAsync(childProfile.Id, familyReward.Id, cancellationToken) is not null)
        {
            throw new ConflictException("A pending redemption already exists for this child and reward.");
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

        return ToRedemptionDto(redemption);
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

        return redemptions.Select(ToRedemptionDto).ToArray();
    }

    public async Task<RedemptionDto> ReviewRedemptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid redemptionId,
        ReviewRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyManagerAsync(currentUserId, familyId, cancellationToken);

        var redemption = await GetRedemptionOrThrowAsync(redemptionId, familyId, cancellationToken);

        if (redemption.Status != RedemptionStatus.Pending)
        {
            throw new ConflictException("Only pending redemptions can be reviewed.");
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

        return ToRedemptionDto(redemption);
    }

    private async Task<Reward> GetRewardOrThrowAsync(Guid rewardId, CancellationToken cancellationToken)
    {
        return await _rewardRepository.GetByIdAsync(rewardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reward), rewardId);
    }

    private async Task<RewardRedemption> GetRedemptionOrThrowAsync(Guid redemptionId, Guid familyId, CancellationToken cancellationToken)
    {
        var redemption = await _rewardRedemptionRepository.GetByIdAsync(redemptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(RewardRedemption), redemptionId);

        if (redemption.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(RewardRedemption), redemptionId);
        }

        return redemption;
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

    private async Task<FamilyMember> EnsureFamilyManagerAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Parent or FamilyAdmin role is required.");
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

    private static Reward ResolveRewardForFamily(Reward redemptionReward, Guid familyId)
    {
        if (redemptionReward.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(Reward), redemptionReward.Id);
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

    private static void EnsureSuperAdmin(Guid currentUserId, string? currentUserRole)
    {
        EnsureAuthenticated(currentUserId);

        if (!string.Equals(currentUserRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("SuperAdmin role is required.");
        }
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
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
