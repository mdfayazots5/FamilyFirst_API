using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class AdminService : IAdminService
{
    private static readonly HashSet<string> AllowedSubscriptionStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active",
        "Trial",
        "Expired",
        "Cancelled"
    };

    private readonly IAdminRepository _adminRepository;
    private readonly INotificationService _notificationService;

    public AdminService(
        IAdminRepository adminRepository,
        INotificationService notificationService)
    {
        _adminRepository = adminRepository;
        _notificationService = notificationService;
    }

    public Task<AdminDashboardDto> GetDashboardAsync(string? currentUserRole, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);
        return _adminRepository.GetDashboardAsync(cancellationToken);
    }

    public async Task<PaginatedList<AdminFamilySummaryDto>> SearchFamiliesAsync(
        string? currentUserRole,
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);
        var families = await _adminRepository.SearchFamiliesAsync(request, cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        return PaginatedList<AdminFamilySummaryDto>.Create(families, page, pageSize);
    }

    public async Task<AdminFamilyDetailDto> GetFamilyDetailAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        return await _adminRepository.GetFamilyDetailAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Family), familyId);
    }

    public async Task<AdminFamilyDetailDto> UpdateFamilySubscriptionAsync(
        string? currentUserRole,
        Guid familyId,
        UpdateFamilySubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        var family = await _adminRepository.GetFamilyEntityByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Family), familyId);
        var subscription = await _adminRepository.GetSubscriptionEntityByFamilyIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Subscription), familyId);
        var plan = await _adminRepository.GetPlanEntityByIdAsync(request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Plan), request.PlanId);

        family.PlanId = plan.InternalId;
        subscription.PlanId = plan.InternalId;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!AllowedSubscriptionStatuses.Contains(request.Status))
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["Status"] = new[] { "Status must be Active, Trial, Expired, or Cancelled." }
                    });
            }

            subscription.Status = request.Status.Trim();
        }

        if (request.ExtendTrialDays.GetValueOrDefault() > 0)
        {
            var baseDate = subscription.TrialEndDate ?? DateTime.UtcNow;
            subscription.TrialEndDate = baseDate.AddDays(request.ExtendTrialDays!.Value);
            subscription.Status = "Trial";
        }

        await _adminRepository.UpdateFamilyAndSubscriptionAsync(family, subscription, cancellationToken);

        return await GetFamilyDetailAsync(currentUserRole, familyId, cancellationToken);
    }

    public async Task<bool> BlockFamilyAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        var family = await _adminRepository.GetFamilyEntityByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Family), familyId);
        var members = await _adminRepository.ListFamilyMemberEntitiesAsync(familyId, cancellationToken);

        family.IsActive = false;

        foreach (var member in members)
        {
            member.IsActive = false;
        }

        await _adminRepository.UpdateFamilyAndMembersAsync(family, members, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<AdminPlanDto>> ListPlansAsync(
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);
        return await _adminRepository.ListPlansAsync(cancellationToken);
    }

    public async Task<AdminPlanDto> UpdatePlanAsync(
        string? currentUserRole,
        int planId,
        UpdatePlanRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        var plan = await _adminRepository.GetPlanEntityByIdAsync(planId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Plan), planId);
        plan.PlanName = request.PlanName.Trim();
        plan.PriceMonthly = request.PriceMonthly;
        plan.MaxChildren = request.MaxChildren;
        plan.MaxTeachers = request.MaxTeachers;
        plan.HasElderMode = request.HasElderMode;
        plan.HasWeeklyDigest = request.HasWeeklyDigest;
        plan.HasAdvancedReports = request.HasAdvancedReports;
        plan.StorageQuotaMb = request.StorageQuotaMb;
        plan.TrialDays = request.TrialDays;
        plan.IsActive = request.IsActive;

        await _adminRepository.UpdatePlanAsync(plan, cancellationToken);

        return new AdminPlanDto(
            (int)plan.InternalId,
            plan.PlanName,
            plan.PlanCode,
            plan.PriceMonthly,
            plan.MaxChildren,
            plan.MaxTeachers,
            plan.HasElderMode,
            plan.HasWeeklyDigest,
            plan.HasAdvancedReports,
            plan.StorageQuotaMb,
            plan.TrialDays,
            plan.IsActive);
    }

    public Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);
        return _adminRepository.GetAnalyticsOverviewAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FeatureFlagDto>> ListFeatureFlagsAsync(
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);
        return await _adminRepository.ListFeatureFlagsAsync(cancellationToken);
    }

    public async Task<FeatureFlagDto> UpdateFeatureFlagAsync(
        string? currentUserRole,
        string flagKey,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        var featureFlag = await _adminRepository.GetFeatureFlagEntityByKeyAsync(flagKey, cancellationToken);

        if (featureFlag is null)
        {
            featureFlag = new Domain.Entities.FeatureFlag
            {
                FlagKey = flagKey,
                FlagValue = request.FlagValue.Trim(),
                Description = NormalizeOptional(request.Description),
                LastUpdated = DateTime.UtcNow
            };

            await _adminRepository.AddFeatureFlagAsync(featureFlag, cancellationToken);
        }
        else
        {
            featureFlag.FlagValue = request.FlagValue.Trim();
            featureFlag.Description = NormalizeOptional(request.Description);
            featureFlag.LastUpdated = DateTime.UtcNow;
            await _adminRepository.UpdateFeatureFlagAsync(featureFlag, cancellationToken);
        }

        return new FeatureFlagDto(
            featureFlag.FlagKey,
            featureFlag.FlagValue,
            featureFlag.Description,
            featureFlag.LastUpdated ?? DateTime.MinValue);
    }

    public async Task<NotificationCampaignResultDto> SendCampaignAsync(
        string? currentUserRole,
        NotificationCampaignRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserRole);

        var recipientUserIds = await _adminRepository.ListCampaignRecipientUserIdsAsync(
            request.Roles,
            request.PlanCodes,
            cancellationToken);
        var notifications = recipientUserIds
            .Select(recipientUserId => new CreateNotificationRequest
            {
                RecipientUserId = recipientUserId,
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                Priority = request.Priority,
                ReferenceType = "Campaign",
                DeepLinkPath = NormalizeOptional(request.DeepLinkPath),
                ScheduledFor = request.ScheduledFor
            })
            .ToArray();

        if (notifications.Length > 0)
        {
            await _notificationService.CreateManyAsync(notifications, cancellationToken);
        }

        return new NotificationCampaignResultDto(notifications.Length);
    }

    private static void EnsureSuperAdmin(string? currentUserRole)
    {
        if (!string.Equals(currentUserRole, Domain.Enums.UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("SuperAdmin role is required.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
