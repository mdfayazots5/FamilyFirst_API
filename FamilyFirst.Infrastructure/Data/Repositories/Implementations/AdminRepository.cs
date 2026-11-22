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
        var signupsToday = await _dbContext.Families.CountAsync(
            family => family.DateCreated >= today && family.DateCreated < today.AddDays(1),
            cancellationToken);

        return new AdminDashboardDto(totalFamilies, activeFamilies, signupsToday);
    }

    public async Task<IReadOnlyCollection<AdminFamilySummaryDto>> SearchFamiliesAsync(
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken)
    {
        var families = await _dbContext.Families
            .OrderByDescending(family => family.DateCreated)
            .ThenByDescending(family => family.InternalId)
            .ToArrayAsync(cancellationToken);
        var memberCounts = await _dbContext.FamilyMembers
            .Where(member => member.IsActive && member.Role != UserRole.SuperAdmin)
            .GroupBy(member => member.FamilyId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var status = request.Status?.Trim().ToUpperInvariant();
        return families
            .Where(family => string.IsNullOrWhiteSpace(request.Query)
                || family.FamilyName.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)
                || (family.City?.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(family => !request.IsActive.HasValue || family.IsActive == request.IsActive.Value)
            .Where(family => status switch
            {
                "ACTIVE" => family.IsActive,
                "FLAGGED" => !family.IsActive,
                _ => true
            })
            .Select(family => new AdminFamilySummaryDto(
                family.Id,
                family.FamilyName,
                family.City,
                family.IsActive,
                memberCounts.GetValueOrDefault(family.InternalId, 0),
                family.CreatedAt))
            .ToArray();
    }

    public async Task<AdminFamilyDetailDto?> GetFamilyDetailAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var family = await _dbContext.Families
            .SingleOrDefaultAsync(item => item.Id == familyId, cancellationToken);

        if (family is null)
        {
            return null;
        }

        var members = await _dbContext.FamilyMembers
            .Include(member => member.User)
            .Where(member => member.Family!.Id == familyId && member.Role != UserRole.SuperAdmin)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);

        return new AdminFamilyDetailDto(
            family.Id,
            family.FamilyName,
            family.City,
            family.IsActive,
            family.FamilyScore,
            family.CurrentStreakDays,
            family.CreatedAt,
            members
                .Select(member => new AdminFamilyMemberDto(
                    member.Id,
                    member.User?.Id ?? Guid.Empty,
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

    public async Task<IReadOnlyCollection<FamilyMember>> ListFamilyMemberEntitiesAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FamilyMembers
            .Where(member => member.Family!.Id == familyId)
            .ToArrayAsync(cancellationToken);
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
                featureFlag.LastUpdated ?? featureFlag.DateCreated))
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
        var query = _dbContext.FamilyMembers
            .Include(member => member.Family)
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

        return await query
            .Select(member => member.User!.Id)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
