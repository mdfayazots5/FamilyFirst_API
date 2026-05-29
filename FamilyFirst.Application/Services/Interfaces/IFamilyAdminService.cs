using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IFamilyAdminService
{
    Task<FamilyAdminPanelDto> GetPanelAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ModuleVisibilityDto>> GetModuleVisibilityAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ModuleVisibilityDto>> UpdateModuleVisibilityAsync(
        Guid currentUserId,
        Guid familyId,
        UpdateModuleVisibilityRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationRuleDto>> GetNotificationRulesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<NotificationRuleDto> UpdateNotificationRuleAsync(
        Guid currentUserId,
        Guid familyId,
        Guid ruleId,
        UpdateNotificationRuleRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CustomAttendanceStatusDto>> GetAttendanceStatusesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<CustomAttendanceStatusDto> CreateAttendanceStatusAsync(
        Guid currentUserId,
        Guid familyId,
        CreateCustomAttendanceStatusRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAttendanceStatusAsync(
        Guid currentUserId,
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken);
}

public interface IFamilyAdminConfigRepository
{
    Task<Family?> GetFamilyByIdAsync(Guid familyId, CancellationToken cancellationToken);

    Task<FamilyMember?> GetActiveFamilyMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FamilyMember>> ListActiveFamilyMembersAsync(Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ModuleVisibilityConfig>> ListModuleVisibilityConfigsAsync(
        Guid familyId,
        CancellationToken cancellationToken);

    Task<ModuleVisibilityConfig?> GetModuleVisibilityConfigAsync(
        Guid familyId,
        UserRole role,
        string moduleName,
        CancellationToken cancellationToken);

    Task AddModuleVisibilityConfigAsync(ModuleVisibilityConfig config, CancellationToken cancellationToken);

    Task UpdateModuleVisibilityConfigAsync(ModuleVisibilityConfig config, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationRule>> ListNotificationRulesAsync(Guid familyId, CancellationToken cancellationToken);

    Task<NotificationRule?> GetNotificationRuleByIdAsync(Guid familyId, Guid ruleId, CancellationToken cancellationToken);

    Task<NotificationRule?> GetNotificationRuleByKeyAsync(Guid familyId, string ruleKey, CancellationToken cancellationToken);

    Task AddNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken);

    Task UpdateNotificationRuleAsync(NotificationRule rule, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CustomAttendanceStatus>> ListCustomAttendanceStatusesAsync(
        Guid familyId,
        CancellationToken cancellationToken);

    Task<CustomAttendanceStatus?> GetCustomAttendanceStatusByIdAsync(
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken);

    Task AddCustomAttendanceStatusAsync(CustomAttendanceStatus status, CancellationToken cancellationToken);

    Task DeleteCustomAttendanceStatusAsync(CustomAttendanceStatus status, CancellationToken cancellationToken);

    Task<FamilyAdminPanelStatsDto> GetFamilyAdminPanelStatsAsync(Guid familyId, DateTime weekStartUtc, DateTime weekEndUtc, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FamilyAdminPanelMemberDto>> GetFamilyAdminPanelMembersAsync(Guid familyId, DateTime weekStartUtc, DateTime weekEndUtc, CancellationToken cancellationToken);
}
