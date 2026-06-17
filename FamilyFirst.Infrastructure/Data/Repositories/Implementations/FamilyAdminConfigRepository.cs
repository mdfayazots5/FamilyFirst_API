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
                member => member.Family!.Id == familyId && member.User!.Id == userId && member.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<FamilyMember>> ListActiveFamilyMembersAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.FamilyMembers
            .Include(member => member.User)
            .Where(member => member.Family!.Id == familyId && member.IsActive)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModuleVisibilityConfig>> ListModuleVisibilityConfigsAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ModuleVisibilityConfigs
            .Where(config => config.FamilyId == null || config.Family!.Id == familyId)
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
                config.Family!.Id == familyId
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
            .Where(rule => rule.Family!.Id == familyId)
            .OrderBy(rule => rule.RuleKey)
            .ToArrayAsync(cancellationToken);
    }

    public Task<NotificationRule?> GetNotificationRuleByIdAsync(Guid familyId, Guid ruleId, CancellationToken cancellationToken)
    {
        return _dbContext.NotificationRules.SingleOrDefaultAsync(
            rule => rule.Family!.Id == familyId && rule.RuleId == ruleId,
            cancellationToken);
    }

    public Task<NotificationRule?> GetNotificationRuleByKeyAsync(Guid familyId, string ruleKey, CancellationToken cancellationToken)
    {
        return _dbContext.NotificationRules.SingleOrDefaultAsync(
            rule => rule.Family!.Id == familyId && rule.RuleKey == ruleKey,
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
            .Where(status => status.Family!.Id == familyId)
            .OrderBy(status => status.StatusName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CustomAttendanceStatus?> GetCustomAttendanceStatusByIdAsync(
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CustomAttendanceStatuses.SingleOrDefaultAsync(
            status => status.Family!.Id == familyId && status.StatusId == statusId,
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

    // ── Level 2 Admin Config ───────────────────────────────────────────────────

    public Task<VaultFamilySettings?> GetVaultFamilySettingsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<VaultFamilySettings>()
            .SingleOrDefaultAsync(s => s.Family.Id == familyId, cancellationToken);
    }

    public async Task UpsertVaultFamilySettingsAsync(VaultFamilySettings settings, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Set<VaultFamilySettings>()
            .SingleOrDefaultAsync(s => s.FamilyId == settings.FamilyId, cancellationToken);

        if (existing is null)
        {
            _dbContext.Set<VaultFamilySettings>().Add(settings);
        }
        else
        {
            // Copy all configurable fields
            existing.StorageMode                     = settings.StorageMode;
            existing.StorageQuotaAlertThresholdPct   = settings.StorageQuotaAlertThresholdPct;
            existing.OfflineCacheSizeMb              = settings.OfflineCacheSizeMb;
            existing.HybridRoutingJson               = settings.HybridRoutingJson;
            existing.EmergencyAccessMode             = settings.EmergencyAccessMode;
            existing.EmergencyLinkExpiryHours        = settings.EmergencyLinkExpiryHours;
            existing.EmergencyContactsJson           = settings.EmergencyContactsJson;
            existing.FinanceLargeTransactionThreshold = settings.FinanceLargeTransactionThreshold;
            existing.DocExpiryLeadDaysDefault        = settings.DocExpiryLeadDaysDefault;
            existing.DocExpiryLeadDaysIdentity       = settings.DocExpiryLeadDaysIdentity;
            existing.DocExpiryLeadDaysMedical        = settings.DocExpiryLeadDaysMedical;
            existing.DocExpiryLeadDaysInsurance      = settings.DocExpiryLeadDaysInsurance;
            existing.LateArrivalToleranceMinutes     = settings.LateArrivalToleranceMinutes;
            existing.LocationStaleThresholdMinutes   = settings.LocationStaleThresholdMinutes;
            existing.DefaultAdultEarningMemberTier   = settings.DefaultAdultEarningMemberTier;
            existing.DefaultIndependentMemberTier    = settings.DefaultIndependentMemberTier;
            existing.ConsentReminderIntervalDays     = settings.ConsentReminderIntervalDays;
            existing.AutoExcludeSalaryCredits        = settings.AutoExcludeSalaryCredits;
            existing.UpdatedAt                       = DateTime.UtcNow;
            _dbContext.Set<VaultFamilySettings>().Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FamilyAdminPanelStatsDto> GetFamilyAdminPanelStatsAsync(
        Guid familyId,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.FamilyMembers
            .Where(member => member.Family!.Id == familyId && member.IsActive)
            .ToArrayAsync(cancellationToken);
        var attendanceCount = await _dbContext.AttendanceRecords
            .CountAsync(
                record => record.Family!.Id == familyId
                    && record.MarkedAt >= weekStartUtc
                    && record.MarkedAt < weekEndUtc,
                cancellationToken);
        var taskCompletionsCount = await _dbContext.TaskCompletions
            .CountAsync(
                completion => completion.Family!.Id == familyId
                    && completion.SubmittedAt.HasValue
                    && completion.SubmittedAt.Value >= weekStartUtc
                    && completion.SubmittedAt.Value < weekEndUtc,
                cancellationToken);
        var feedbackCount = await _dbContext.TeacherFeedback
            .CountAsync(
                feedback => feedback.Family!.Id == familyId
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
            .Where(member => member.Family!.Id == familyId && member.IsActive)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
        var childProfiles = await _dbContext.ChildProfiles
            .Where(child => child.Family!.Id == familyId)
            .ToDictionaryAsync(child => child.FamilyMember!.Id, child => child.Id, cancellationToken);
        var childIds = childProfiles.Values.ToArray();
        var attendanceCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.AttendanceRecords
                .Where(record => childIds.Contains(record.ChildProfile!.Id)
                    && record.MarkedAt >= weekStartUtc
                    && record.MarkedAt < weekEndUtc)
                .GroupBy(record => record.ChildProfile!.Id)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var taskCompletionCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.TaskCompletions
                .Where(completion => childIds.Contains(completion.ChildProfile!.Id)
                    && completion.SubmittedAt.HasValue
                    && completion.SubmittedAt.Value >= weekStartUtc
                    && completion.SubmittedAt.Value < weekEndUtc)
                .GroupBy(completion => completion.ChildProfile!.Id)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var feedbackCounts = childIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.TeacherFeedback
                .Where(feedback => childIds.Contains(feedback.ChildProfile!.Id)
                    && feedback.CreatedAt >= weekStartUtc
                    && feedback.CreatedAt < weekEndUtc)
                .GroupBy(feedback => feedback.ChildProfile!.Id)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return members
            .Select(member =>
            {
                var childProfileId = childProfiles.GetValueOrDefault(member.Id);

                return new FamilyAdminPanelMemberDto(
                    member.Id,
                    member.User?.Id ?? Guid.Empty,
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
