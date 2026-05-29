using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class AdminRepository : IAdminRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public AdminRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var totalFamilies = await _dbContext.Families.CountAsync(cancellationToken);
        var activeFamilies = await _dbContext.Families.CountAsync(family => family.IsActive, cancellationToken);
        var revenueMonthly = await _dbContext.Subscriptions
            .Where(subscription => subscription.Status == "Active" || subscription.Status == "Trial")
            .Join(
                _dbContext.Plans,
                subscription => subscription.PlanId,
                plan => plan.PlanId,
                (_, plan) => plan.PriceMonthly)
            .DefaultIfEmpty(0m)
            .SumAsync(cancellationToken);
        var churnCount = await _dbContext.Subscriptions.CountAsync(
            subscription => subscription.Status == "Expired" || subscription.Status == "Cancelled",
            cancellationToken);
        var signupsToday = await _dbContext.Families.CountAsync(
            family => family.CreatedAt >= today && family.CreatedAt < today.AddDays(1),
            cancellationToken);

        return new AdminDashboardDto(totalFamilies, activeFamilies, revenueMonthly, churnCount, signupsToday);
    }

    public async Task<IReadOnlyCollection<AdminFamilySummaryDto>> SearchFamiliesAsync(
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken)
    {
        var families = await _dbContext.Families
            .Include(family => family.Plan)
            .OrderByDescending(family => family.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var subscriptions = await _dbContext.Subscriptions.ToArrayAsync(cancellationToken);
        var memberCounts = await _dbContext.FamilyMembers
            .Where(member => member.IsActive)
            .GroupBy(member => member.FamilyId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return families
            .Where(family => string.IsNullOrWhiteSpace(request.Query)
                || family.FamilyName.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)
                || family.JoinCode.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)
                || (family.City?.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(family => string.IsNullOrWhiteSpace(request.PlanCode)
                || string.Equals(family.Plan?.PlanCode, request.PlanCode.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(family => !request.IsActive.HasValue || family.IsActive == request.IsActive.Value)
            .Select(family =>
            {
                var subscription = subscriptions.FirstOrDefault(item => item.FamilyId == family.Id);

                return new AdminFamilySummaryDto(
                    family.Id,
                    family.FamilyName,
                    family.City,
                    family.Plan?.PlanCode ?? string.Empty,
                    family.Plan?.PlanName ?? string.Empty,
                    subscription?.Status ?? "Unknown",
                    family.IsActive,
                    memberCounts.GetValueOrDefault(family.Id),
                    family.CreatedAt);
            })
            .ToArray();
    }

    public async Task<AdminFamilyDetailDto?> GetFamilyDetailAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var family = await _dbContext.Families
            .Include(item => item.Plan)
            .SingleOrDefaultAsync(item => item.Id == familyId, cancellationToken);

        if (family is null)
        {
            return null;
        }

        var subscription = await _dbContext.Subscriptions
            .SingleOrDefaultAsync(item => item.FamilyId == familyId, cancellationToken);
        var members = await _dbContext.FamilyMembers
            .Include(member => member.User)
            .Where(member => member.FamilyId == familyId)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);

        return new AdminFamilyDetailDto(
            family.Id,
            family.FamilyName,
            family.JoinCode,
            family.City,
            family.IsActive,
            family.FamilyScore,
            family.CurrentStreakDays,
            family.CreatedAt,
            family.PlanId,
            family.Plan?.PlanCode ?? string.Empty,
            family.Plan?.PlanName ?? string.Empty,
            subscription?.Id,
            subscription?.Status,
            subscription?.TrialEndDate,
            subscription?.EndDate,
            members
                .Select(member => new AdminFamilyMemberDto(
                    member.Id,
                    member.UserId,
                    member.User?.FullName ?? member.DisplayName ?? "User",
                    member.User?.PhoneNumber ?? string.Empty,
                    member.Role.ToString(),
                    member.IsActive,
                    member.JoinedAt))
                .ToArray());
    }

    public Task<Family?> GetFamilyEntityByIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Families.SingleOrDefaultAsync(family => family.Id == familyId, cancellationToken);
    }

    public Task<Subscription?> GetSubscriptionEntityByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Subscriptions.SingleOrDefaultAsync(subscription => subscription.FamilyId == familyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FamilyMember>> ListFamilyMemberEntitiesAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FamilyMembers
            .Where(member => member.FamilyId == familyId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateFamilyAndSubscriptionAsync(
        Family family,
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Families.Update(family);
        _dbContext.Subscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateFamilyAndMembersAsync(
        Family family,
        IReadOnlyCollection<FamilyMember> members,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Families.Update(family);
        _dbContext.FamilyMembers.UpdateRange(members);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminPlanDto>> ListPlansAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Plans
            .OrderBy(plan => plan.PlanId)
            .Select(plan => new AdminPlanDto(
                plan.PlanId,
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
                plan.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    public Task<Plan?> GetPlanEntityByIdAsync(int planId, CancellationToken cancellationToken)
    {
        return _dbContext.Plans.SingleOrDefaultAsync(plan => plan.PlanId == planId, cancellationToken);
    }

    public async Task UpdatePlanAsync(Plan plan, CancellationToken cancellationToken)
    {
        _dbContext.Plans.Update(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(CancellationToken cancellationToken)
    {
        return new AnalyticsOverviewDto(
            await _dbContext.Users.CountAsync(cancellationToken),
            await _dbContext.ChildProfiles.CountAsync(cancellationToken),
            await _dbContext.TeacherProfiles.CountAsync(cancellationToken),
            await _dbContext.TaskItems.CountAsync(taskItem => !taskItem.IsSystemTemplate, cancellationToken),
            await _dbContext.TaskCompletions.CountAsync(cancellationToken),
            await _dbContext.TeacherFeedback.CountAsync(cancellationToken),
            await _dbContext.Set<Notification>().CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyCollection<FeatureFlagDto>> ListFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.FeatureFlags
            .OrderBy(featureFlag => featureFlag.FlagKey)
            .Select(featureFlag => new FeatureFlagDto(
                featureFlag.FlagKey,
                featureFlag.FlagValue,
                featureFlag.Description,
                featureFlag.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task<FeatureFlag?> GetFeatureFlagEntityByKeyAsync(string flagKey, CancellationToken cancellationToken)
    {
        return _dbContext.FeatureFlags.SingleOrDefaultAsync(
            featureFlag => featureFlag.FlagKey == flagKey,
            cancellationToken);
    }

    public async Task AddFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken)
    {
        await _dbContext.FeatureFlags.AddAsync(featureFlag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken)
    {
        _dbContext.FeatureFlags.Update(featureFlag);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsMaintenanceModeEnabledAsync(CancellationToken cancellationToken)
    {
        var featureFlag = await _dbContext.FeatureFlags.SingleOrDefaultAsync(
            item => item.FlagKey == "MaintenanceMode",
            cancellationToken);

        return featureFlag is not null
            && bool.TryParse(featureFlag.FlagValue, out var isEnabled)
            && isEnabled;
    }

    public async Task<IReadOnlyCollection<Guid>> ListCampaignRecipientUserIdsAsync(
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> planCodes,
        CancellationToken cancellationToken)
    {
        var normalizedRoleValues = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => Enum.TryParse<UserRole>(role.Trim(), true, out var parsedRole)
                ? parsedRole
                : (UserRole?)null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .ToHashSet();
        var normalizedPlanCodes = planCodes
            .Where(planCode => !string.IsNullOrWhiteSpace(planCode))
            .Select(planCode => planCode.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var query = _dbContext.FamilyMembers
            .Include(member => member.Family)
            .ThenInclude(family => family!.Plan)
            .Include(member => member.User)
            .Where(member =>
                member.IsActive
                && member.Family != null
                && member.Family.IsActive
                && member.User != null
                && member.User.IsActive);

        if (normalizedRoleValues.Count > 0)
        {
            query = query.Where(member => normalizedRoleValues.Contains(member.Role));
        }

        if (normalizedPlanCodes.Count > 0)
        {
            query = query.Where(member => member.Family!.Plan != null && normalizedPlanCodes.Contains(member.Family.Plan.PlanCode));
        }

        return await query
            .Select(member => member.UserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
