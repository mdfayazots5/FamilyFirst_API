using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class FamilyAdminConfigRepository : IFamilyAdminConfigRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public FamilyAdminConfigRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Family?> GetFamilyByIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Families.SingleOrDefaultAsync(family => family.Id == familyId, cancellationToken);
    }

    public Task<FamilyMember?> GetActiveFamilyMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.FamilyMembers
            .Include(member => member.User)
            .SingleOrDefaultAsync(
                member => member.FamilyId == familyId && member.UserId == userId && member.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<FamilyMember>> ListActiveFamilyMembersAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.FamilyMembers
            .Include(member => member.User)
            .Where(member => member.FamilyId == familyId && member.IsActive)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModuleVisibilityConfig>> ListModuleVisibilityConfigsAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ModuleVisibilityConfigs
            .Where(config => config.FamilyId == null || config.FamilyId == familyId)
            .OrderBy(config => config.RoleId)
            .ThenBy(config => config.ModuleName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<ModuleVisibilityConfig?> GetModuleVisibilityConfigAsync(
        Guid familyId,
        UserRole role,
        string moduleName,
        CancellationToken cancellationToken)
    {
        return _dbContext.ModuleVisibilityConfigs.SingleOrDefaultAsync(
            config =>
                config.FamilyId == familyId
                && config.RoleId == (int)role
                && config.ModuleName == moduleName,
            cancellationToken);
    }

    public async Task AddModuleVisibilityConfigAsync(ModuleVisibilityConfig config, CancellationToken cancellationToken)
    {
        await _dbContext.ModuleVisibilityConfigs.AddAsync(config, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateModuleVisibilityConfigAsync(ModuleVisibilityConfig config, CancellationToken cancellationToken)
    {
        _dbContext.ModuleVisibilityConfigs.Update(config);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<NotificationRule>> ListNotificationRulesAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.NotificationRules
            .Where(rule => rule.FamilyId == familyId)
            .OrderBy(rule => rule.RuleKey)
            .ToArrayAsync(cancellationToken);
    }

    public Task<NotificationRule?> GetNotificationRuleByIdAsync(Guid familyId, Guid ruleId, CancellationToken cancellationToken)
    {
        return _dbContext.NotificationRules.SingleOrDefaultAsync(
            rule => rule.FamilyId == familyId && rule.RuleId == ruleId,
            cancellationToken);
    }

    public Task<NotificationRule?> GetNotificationRuleByKeyAsync(Guid familyId, string ruleKey, CancellationToken cancellationToken)
    {
        return _dbContext.NotificationRules.SingleOrDefaultAsync(
            rule => rule.FamilyId == familyId && rule.RuleKey == ruleKey,
            cancellationToken);
    }

    public async Task AddNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken)
    {
        await _dbContext.NotificationRules.AddAsync(rule, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken)
    {
        _dbContext.NotificationRules.Update(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CustomAttendanceStatus>> ListCustomAttendanceStatusesAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CustomAttendanceStatuses
            .Where(status => status.FamilyId == familyId)
            .OrderBy(status => status.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CustomAttendanceStatus?> GetCustomAttendanceStatusByIdAsync(
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CustomAttendanceStatuses.SingleOrDefaultAsync(
            status => status.FamilyId == familyId && status.StatusId == statusId,
            cancellationToken);
    }

    public async Task AddCustomAttendanceStatusAsync(CustomAttendanceStatus status, CancellationToken cancellationToken)
    {
        await _dbContext.CustomAttendanceStatuses.AddAsync(status, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCustomAttendanceStatusAsync(CustomAttendanceStatus status, CancellationToken cancellationToken)
    {
        _dbContext.CustomAttendanceStatuses.Remove(status);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FamilyAdminPanelStatsDto> GetFamilyAdminPanelStatsAsync(
        Guid familyId,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.FamilyMembers
            .Where(member => member.FamilyId == familyId && member.IsActive)
            .ToArrayAsync(cancellationToken);
        var attendanceCount = await _dbContext.AttendanceRecords
            .CountAsync(
                record => record.FamilyId == familyId
                    && record.MarkedAt >= weekStartUtc
                    && record.MarkedAt < weekEndUtc,
                cancellationToken);
        var taskCompletionsCount = await _dbContext.TaskCompletions
            .CountAsync(
                completion => completion.FamilyId == familyId
                    && completion.SubmittedAt.HasValue
                    && completion.SubmittedAt.Value >= weekStartUtc
                    && completion.SubmittedAt.Value < weekEndUtc,
                cancellationToken);
        var feedbackCount = await _dbContext.TeacherFeedback
            .CountAsync(
                feedback => feedback.FamilyId == familyId
                    && feedback.CreatedAt >= weekStartUtc
                    && feedback.CreatedAt < weekEndUtc,
                cancellationToken);

        return new FamilyAdminPanelStatsDto(
            members.Length,
            members.Count(member => member.Role == UserRole.Parent),
            members.Count(member => member.Role == UserRole.Child),
            members.Count(member => member.Role == UserRole.Teacher),
            members.Count(member => member.Role == UserRole.Elder),
            attendanceCount,
            taskCompletionsCount,
            feedbackCount);
    }

    public async Task<IReadOnlyCollection<FamilyAdminPanelMemberDto>> GetFamilyAdminPanelMembersAsync(
        Guid familyId,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.FamilyMembers
            .Include(member => member.User)
            .Where(member => member.FamilyId == familyId && member.IsActive)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
        var childProfiles = await _dbContext.ChildProfiles
            .Where(child => child.FamilyId == familyId)
            .ToDictionaryAsync(child => child.FamilyMemberId, child => child.Id, cancellationToken);
        var childIds = childProfiles.Values.ToArray();
        var attendanceCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.AttendanceRecords
                .Where(record => childIds.Contains(record.ChildProfileId)
                    && record.MarkedAt >= weekStartUtc
                    && record.MarkedAt < weekEndUtc)
                .GroupBy(record => record.ChildProfileId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var taskCompletionCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.TaskCompletions
                .Where(completion => childIds.Contains(completion.ChildProfileId)
                    && completion.SubmittedAt.HasValue
                    && completion.SubmittedAt.Value >= weekStartUtc
                    && completion.SubmittedAt.Value < weekEndUtc)
                .GroupBy(completion => completion.ChildProfileId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var feedbackCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.TeacherFeedback
                .Where(feedback => childIds.Contains(feedback.ChildProfileId)
                    && feedback.CreatedAt >= weekStartUtc
                    && feedback.CreatedAt < weekEndUtc)
                .GroupBy(feedback => feedback.ChildProfileId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return members
            .Select(member =>
            {
                var childProfileId = childProfiles.GetValueOrDefault(member.Id);

                return new FamilyAdminPanelMemberDto(
                    member.Id,
                    member.UserId,
                    member.User?.FullName ?? member.DisplayName ?? "User",
                    member.Role,
                    member.IsActive,
                    member.JoinedAt,
                    childProfileId == Guid.Empty ? 0 : attendanceCounts.GetValueOrDefault(childProfileId),
                    childProfileId == Guid.Empty ? 0 : taskCompletionCounts.GetValueOrDefault(childProfileId),
                    childProfileId == Guid.Empty ? 0 : feedbackCounts.GetValueOrDefault(childProfileId));
            })
            .ToArray();
    }
}
