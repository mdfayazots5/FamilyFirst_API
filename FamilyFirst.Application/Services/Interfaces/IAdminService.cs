using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync(string? currentUserRole, CancellationToken cancellationToken);

    Task<PaginatedList<AdminFamilySummaryDto>> SearchFamiliesAsync(
        string? currentUserRole,
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken);

    Task<AdminFamilyDetailDto> GetFamilyDetailAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<AdminFamilyDetailDto> UpdateFamilySubscriptionAsync(
        string? currentUserRole,
        Guid familyId,
        UpdateFamilySubscriptionRequest request,
        CancellationToken cancellationToken);

    Task<bool> BlockFamilyAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminPlanDto>> ListPlansAsync(
        string? currentUserRole,
        CancellationToken cancellationToken);

    Task<AdminPlanDto> UpdatePlanAsync(
        string? currentUserRole,
        int planId,
        UpdatePlanRequest request,
        CancellationToken cancellationToken);

    Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(
        string? currentUserRole,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FeatureFlagDto>> ListFeatureFlagsAsync(
        string? currentUserRole,
        CancellationToken cancellationToken);

    Task<FeatureFlagDto> UpdateFeatureFlagAsync(
        string? currentUserRole,
        string flagKey,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken);

    Task<NotificationCampaignResultDto> SendCampaignAsync(
        string? currentUserRole,
        NotificationCampaignRequest request,
        CancellationToken cancellationToken);
}

public interface IAdminRepository
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminFamilySummaryDto>> SearchFamiliesAsync(
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken);

    Task<AdminFamilyDetailDto?> GetFamilyDetailAsync(Guid familyId, CancellationToken cancellationToken);

    Task<Family?> GetFamilyEntityByIdAsync(Guid familyId, CancellationToken cancellationToken);

    Task<Subscription?> GetSubscriptionEntityByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FamilyMember>> ListFamilyMemberEntitiesAsync(Guid familyId, CancellationToken cancellationToken);

    Task UpdateFamilyAndSubscriptionAsync(Family family, Subscription subscription, CancellationToken cancellationToken);

    Task UpdateFamilyAndMembersAsync(
        Family family,
        IReadOnlyCollection<FamilyMember> members,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminPlanDto>> ListPlansAsync(CancellationToken cancellationToken);

    Task<Plan?> GetPlanEntityByIdAsync(int planId, CancellationToken cancellationToken);

    Task UpdatePlanAsync(Plan plan, CancellationToken cancellationToken);

    Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FeatureFlagDto>> ListFeatureFlagsAsync(CancellationToken cancellationToken);

    Task<FeatureFlag?> GetFeatureFlagEntityByKeyAsync(string flagKey, CancellationToken cancellationToken);

    Task AddFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken);

    Task UpdateFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken);

    Task<bool> IsMaintenanceModeEnabledAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> ListCampaignRecipientUserIdsAsync(
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> planCodes,
        CancellationToken cancellationToken);
}
