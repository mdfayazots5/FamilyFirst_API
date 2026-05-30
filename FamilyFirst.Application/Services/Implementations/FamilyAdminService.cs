using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class FamilyAdminService : IFamilyAdminService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<UserRole, int> RoleLevels = new Dictionary<UserRole, int>
    {
        [UserRole.Elder] = 1,
        [UserRole.Child] = 2,
        [UserRole.Parent] = 3,
        [UserRole.Teacher] = 3,
        [UserRole.FamilyAdmin] = 4,
        [UserRole.SuperAdmin] = 5
    };

    private static readonly IReadOnlyCollection<(UserRole Role, string ModuleName, bool IsVisible)> DefaultModuleVisibility =
    [
        (UserRole.FamilyAdmin, "Family", true),
        (UserRole.FamilyAdmin, "Children", true),
        (UserRole.FamilyAdmin, "Attendance", true),
        (UserRole.FamilyAdmin, "Tasks", true),
        (UserRole.FamilyAdmin, "Rewards", true),
        (UserRole.FamilyAdmin, "Feedback", true),
        (UserRole.FamilyAdmin, "Calendar", true),
        (UserRole.FamilyAdmin, "Reports", true),
        (UserRole.FamilyAdmin, "Notifications", true),
        (UserRole.FamilyAdmin, "FamilyAdmin", true),
        (UserRole.Parent, "Family", true),
        (UserRole.Parent, "Children", true),
        (UserRole.Parent, "Attendance", true),
        (UserRole.Parent, "Tasks", true),
        (UserRole.Parent, "Rewards", true),
        (UserRole.Parent, "Feedback", true),
        (UserRole.Parent, "Calendar", true),
        (UserRole.Parent, "Reports", true),
        (UserRole.Parent, "Notifications", true),
        (UserRole.Child, "Children", true),
        (UserRole.Child, "Attendance", true),
        (UserRole.Child, "Tasks", true),
        (UserRole.Child, "Rewards", true),
        (UserRole.Child, "Calendar", true),
        (UserRole.Teacher, "Attendance", true),
        (UserRole.Teacher, "Feedback", true),
        (UserRole.Teacher, "Calendar", true),
        (UserRole.Teacher, "Notifications", true),
        (UserRole.Elder, "Family", true),
        (UserRole.Elder, "Calendar", true),
        (UserRole.Elder, "Notifications", true)
    ];

    private static readonly IReadOnlyCollection<(string RuleKey, bool IsEnabled)> DefaultNotificationRules =
    [
        ("Attendance", true),
        ("Feedback", true),
        ("Task", true),
        ("Reward", true),
        ("Calendar", true),
        ("WeeklyDigest", true)
    ];

    private static readonly IReadOnlyCollection<CustomAttendanceStatusDto> DefaultAttendanceStatuses =
    [
        new CustomAttendanceStatusDto(Guid.Empty, null, AttendanceStatus.Present.ToString(), "#16A34A", 1, true, DateTime.MinValue),
        new CustomAttendanceStatusDto(Guid.Empty, null, AttendanceStatus.Absent.ToString(), "#DC2626", 2, true, DateTime.MinValue),
        new CustomAttendanceStatusDto(Guid.Empty, null, AttendanceStatus.Late.ToString(), "#D97706", 3, true, DateTime.MinValue),
        new CustomAttendanceStatusDto(Guid.Empty, null, AttendanceStatus.LeftEarly.ToString(), "#2563EB", 4, true, DateTime.MinValue)
    ];

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IFamilyAdminConfigRepository _familyAdminConfigRepository;

    public FamilyAdminService(
        IFamilyAdminConfigRepository familyAdminConfigRepository,
        IAuditLogRepository auditLogRepository)
    {
        _familyAdminConfigRepository = familyAdminConfigRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<FamilyAdminPanelDto> GetPanelAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var family = await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var (weekStartUtc, weekEndUtc) = ResolveCurrentWeekRangeUtc();
        var members = await _familyAdminConfigRepository.GetFamilyAdminPanelMembersAsync(
            familyId,
            weekStartUtc,
            weekEndUtc,
            cancellationToken);
        var stats = await _familyAdminConfigRepository.GetFamilyAdminPanelStatsAsync(
            familyId,
            weekStartUtc,
            weekEndUtc,
            cancellationToken);

        return new FamilyAdminPanelDto(family.Id, family.FamilyName, members, stats);
    }

    public async Task<IReadOnlyCollection<ModuleVisibilityDto>> GetModuleVisibilityAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var configs = await _familyAdminConfigRepository.ListModuleVisibilityConfigsAsync(familyId, cancellationToken);

        return BuildEffectiveModuleVisibility(configs);
    }

    public async Task<IReadOnlyCollection<ModuleVisibilityDto>> UpdateModuleVisibilityAsync(
        Guid currentUserId,
        Guid familyId,
        UpdateModuleVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        foreach (var item in request.Items)
        {
            if (item.Role == UserRole.SuperAdmin || RoleLevels[item.Role] > RoleLevels[UserRole.FamilyAdmin])
            {
                throw new ForbiddenAccessException("FamilyAdmin cannot update visibility above their own role level.");
            }

            var existingConfig = await _familyAdminConfigRepository.GetModuleVisibilityConfigAsync(
                familyId,
                item.Role,
                item.ModuleName,
                cancellationToken);

            if (existingConfig is null)
            {
                existingConfig = new ModuleVisibilityConfig
                {
                    FamilyId = familyId,
                    RoleId = (int)item.Role,
                    ModuleName = item.ModuleName.Trim(),
                    IsVisible = item.IsVisible,
                    UpdatedAt = DateTime.UtcNow
                };

                await _familyAdminConfigRepository.AddModuleVisibilityConfigAsync(existingConfig, cancellationToken);
            }
            else
            {
                var oldValues = JsonSerializer.Serialize(existingConfig, AuditJsonOptions);
                existingConfig.IsVisible = item.IsVisible;
                existingConfig.UpdatedAt = DateTime.UtcNow;
                await _familyAdminConfigRepository.UpdateModuleVisibilityConfigAsync(existingConfig, cancellationToken);
                await WriteAuditLogAsync(
                    currentUserId,
                    familyId,
                    "ModuleVisibilityUpdated",
                    nameof(ModuleVisibilityConfig),
                    existingConfig.ConfigId.ToString(),
                    oldValues,
                    JsonSerializer.Serialize(existingConfig, AuditJsonOptions),
                    cancellationToken);
            }
        }

        return await GetModuleVisibilityAsync(currentUserId, familyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<NotificationRuleDto>> GetNotificationRulesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var rules = (await _familyAdminConfigRepository.ListNotificationRulesAsync(familyId, cancellationToken)).ToList();

        foreach (var defaultRule in DefaultNotificationRules.Where(defaultRule =>
                     rules.All(rule => !string.Equals(rule.RuleKey, defaultRule.RuleKey, StringComparison.OrdinalIgnoreCase))))
        {
            var rule = new NotificationRule
            {
                FamilyId = familyId,
                RuleKey = defaultRule.RuleKey,
                IsEnabled = defaultRule.IsEnabled
            };

            await _familyAdminConfigRepository.AddNotificationRuleAsync(rule, cancellationToken);
            rules.Add(rule);
        }

        return BuildEffectiveNotificationRules(familyId, rules);
    }

    public async Task<NotificationRuleDto> UpdateNotificationRuleAsync(
        Guid currentUserId,
        Guid familyId,
        Guid ruleId,
        UpdateNotificationRuleRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var rule = await _familyAdminConfigRepository.GetNotificationRuleByIdAsync(familyId, ruleId, cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationRule), ruleId);
        var oldValues = JsonSerializer.Serialize(rule, AuditJsonOptions);
        rule.IsEnabled = request.IsEnabled;
        rule.PriorityOverride = request.PriorityOverride;
        rule.DeliveryDelayMinutes = request.DeliveryDelayMinutes;
        rule.UpdatedAt = DateTime.UtcNow;
        await _familyAdminConfigRepository.UpdateNotificationRuleAsync(rule, cancellationToken);
        await WriteAuditLogAsync(
            currentUserId,
            familyId,
            "NotificationRuleUpdated",
            nameof(NotificationRule),
            rule.RuleId.ToString(),
            oldValues,
            JsonSerializer.Serialize(rule, AuditJsonOptions),
            cancellationToken);

        return ToNotificationRuleDto(rule);
    }

    public async Task<IReadOnlyCollection<CustomAttendanceStatusDto>> GetAttendanceStatusesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var customStatuses = await _familyAdminConfigRepository.ListCustomAttendanceStatusesAsync(familyId, cancellationToken);

        return DefaultAttendanceStatuses
            .Concat(customStatuses.Select(status => new CustomAttendanceStatusDto(
                status.StatusId,
                status.FamilyId,
                status.StatusName,
                status.ColorHex,
                status.SortOrder,
                false,
                status.CreatedAt)))
            .OrderBy(status => status.SortOrder)
            .ToArray();
    }

    public async Task<CustomAttendanceStatusDto> CreateAttendanceStatusAsync(
        Guid currentUserId,
        Guid familyId,
        CreateCustomAttendanceStatusRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var existingStatuses = await _familyAdminConfigRepository.ListCustomAttendanceStatusesAsync(familyId, cancellationToken);

        if (existingStatuses.Count >= 5)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["AttendanceStatuses"] = ["A family can have at most 5 custom attendance statuses."]
                });
        }

        var nextSortOrder = existingStatuses.Count == 0 ? 5 : existingStatuses.Max(status => status.SortOrder) + 1;
        var status = new CustomAttendanceStatus
        {
            FamilyId = familyId,
            StatusName = request.StatusName.Trim(),
            ColorHex = request.ColorHex.Trim(),
            SortOrder = nextSortOrder
        };
        await _familyAdminConfigRepository.AddCustomAttendanceStatusAsync(status, cancellationToken);
        await WriteAuditLogAsync(
            currentUserId,
            familyId,
            "AttendanceStatusCreated",
            nameof(CustomAttendanceStatus),
            status.StatusId.ToString(),
            null,
            JsonSerializer.Serialize(status, AuditJsonOptions),
            cancellationToken);

        return new CustomAttendanceStatusDto(
            status.StatusId,
            status.FamilyId,
            status.StatusName,
            status.ColorHex,
            status.SortOrder,
            false,
            status.CreatedAt);
    }

    public async Task<bool> DeleteAttendanceStatusAsync(
        Guid currentUserId,
        Guid familyId,
        Guid statusId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var status = await _familyAdminConfigRepository.GetCustomAttendanceStatusByIdAsync(familyId, statusId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomAttendanceStatus), statusId);
        var oldValues = JsonSerializer.Serialize(status, AuditJsonOptions);
        await _familyAdminConfigRepository.DeleteCustomAttendanceStatusAsync(status, cancellationToken);
        await WriteAuditLogAsync(
            currentUserId,
            familyId,
            "AttendanceStatusDeleted",
            nameof(CustomAttendanceStatus),
            status.StatusId.ToString(),
            oldValues,
            null,
            cancellationToken);

        return true;
    }

    private async Task<Family> EnsureFamilyAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }

        var family = await _familyAdminConfigRepository.GetFamilyByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Family), familyId);
        var member = await _familyAdminConfigRepository.GetActiveFamilyMemberAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("FamilyAdmin role is required.");
        }

        return family;
    }

    private async Task EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }

        _ = await _familyAdminConfigRepository.GetActiveFamilyMemberAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private static IReadOnlyCollection<ModuleVisibilityDto> BuildEffectiveModuleVisibility(
        IReadOnlyCollection<ModuleVisibilityConfig> configs)
    {
        var familyConfigs = configs
            .Where(config => config.FamilyId.HasValue)
            .ToDictionary(
                config => $"{config.RoleId}:{config.ModuleName}",
                config => config,
                StringComparer.OrdinalIgnoreCase);
        var defaultConfigs = configs
            .Where(config => !config.FamilyId.HasValue)
            .ToDictionary(
                config => $"{config.RoleId}:{config.ModuleName}",
                config => config,
                StringComparer.OrdinalIgnoreCase);

        return DefaultModuleVisibility
            .Select(defaultItem =>
            {
                var key = $"{(int)defaultItem.Role}:{defaultItem.ModuleName}";

                if (familyConfigs.TryGetValue(key, out var familyConfig))
                {
                    return new ModuleVisibilityDto(
                        familyConfig.ConfigId,
                        defaultItem.Role,
                        familyConfig.ModuleName,
                        familyConfig.IsVisible,
                        false,
                        familyConfig.UpdatedAt);
                }

                if (defaultConfigs.TryGetValue(key, out var defaultConfig))
                {
                    return new ModuleVisibilityDto(
                        defaultConfig.ConfigId,
                        defaultItem.Role,
                        defaultConfig.ModuleName,
                        defaultConfig.IsVisible,
                        true,
                        defaultConfig.UpdatedAt);
                }

                return new ModuleVisibilityDto(
                    null,
                    defaultItem.Role,
                    defaultItem.ModuleName,
                    defaultItem.IsVisible,
                    true,
                    DateTime.MinValue);
            })
            .OrderBy(item => item.Role)
            .ThenBy(item => item.ModuleName)
            .ToArray();
    }

    private static IReadOnlyCollection<NotificationRuleDto> BuildEffectiveNotificationRules(
        Guid familyId,
        IReadOnlyCollection<NotificationRule> rules)
    {
        var ruleLookup = rules.ToDictionary(rule => rule.RuleKey, StringComparer.OrdinalIgnoreCase);

        return DefaultNotificationRules
            .Select(defaultRule =>
            {
                if (ruleLookup.TryGetValue(defaultRule.RuleKey, out var rule))
                {
                    return ToNotificationRuleDto(rule);
                }

                return new NotificationRuleDto(Guid.Empty, familyId, defaultRule.RuleKey, defaultRule.IsEnabled, null, null, DateTime.MinValue);
            })
            .ToArray();
    }

    private static NotificationRuleDto ToNotificationRuleDto(NotificationRule rule)
    {
        return new NotificationRuleDto(
            rule.RuleId,
            rule.FamilyId,
            rule.RuleKey,
            rule.IsEnabled,
            rule.PriorityOverride,
            rule.DeliveryDelayMinutes,
            rule.UpdatedAt);
    }

    private static (DateTime WeekStartUtc, DateTime WeekEndUtc) ResolveCurrentWeekRangeUtc()
    {
        var utcNow = DateTime.UtcNow;
        var offset = ((int)utcNow.DayOfWeek + 6) % 7;
        var weekStart = utcNow.Date.AddDays(-offset);

        return (weekStart, weekStart.AddDays(7));
    }

    // ── Level 2 Admin Config ──────────────────────────────────────────────────

    public async Task<StorageConfigDto> GetStorageConfigAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var settings = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        var routing = DeserializeHybridRouting(settings.HybridRoutingJson);

        return new StorageConfigDto(
            settings.StorageMode,
            false,                      // GoogleDriveConnected — OAuth state not persisted in MVP
            null, null,
            settings.StorageQuotaAlertThresholdPct,
            settings.OfflineCacheSizeMb,
            0L, ResolveQuotaBytes(familyId),
            routing);
    }

    public async Task<StorageConfigDto> UpdateStorageConfigAsync(
        Guid currentUserId, Guid familyId,
        UpdateStorageConfigRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        if (!new[] { "AppManaged", "GoogleDrive", "Hybrid" }.Contains(request.StorageMode))
            throw new ValidationException(new Dictionary<string, string[]>
                { ["StorageMode"] = ["StorageMode must be AppManaged, GoogleDrive, or Hybrid."] });

        var settings = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);
        settings.StorageMode = request.StorageMode;
        if (request.StorageQuotaAlertThresholdPct.HasValue) settings.StorageQuotaAlertThresholdPct = request.StorageQuotaAlertThresholdPct.Value;
        if (request.OfflineCacheSizeMb.HasValue)            settings.OfflineCacheSizeMb = request.OfflineCacheSizeMb.Value;
        if (request.HybridRouting is { Count: > 0 })
            settings.HybridRoutingJson = JsonSerializer.Serialize(request.HybridRouting);

        await _familyAdminConfigRepository.UpsertVaultFamilySettingsAsync(settings, cancellationToken);
        await WriteAuditLogAsync(currentUserId, familyId, "StorageConfigUpdated", "VaultFamilySettings",
            familyId.ToString(), null, $"StorageMode={request.StorageMode}", cancellationToken);

        return await GetStorageConfigAsync(currentUserId, familyId, cancellationToken);
    }

    public async Task<AlertThresholdsDto> GetAlertThresholdsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        return new AlertThresholdsDto(
            s.FinanceLargeTransactionThreshold,
            s.DocExpiryLeadDaysDefault,
            s.DocExpiryLeadDaysIdentity,
            s.DocExpiryLeadDaysMedical,
            s.DocExpiryLeadDaysInsurance,
            s.LateArrivalToleranceMinutes,
            s.LocationStaleThresholdMinutes);
    }

    public async Task<AlertThresholdsDto> UpdateAlertThresholdsAsync(
        Guid currentUserId, Guid familyId,
        UpdateAlertThresholdsRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        if (request.FinanceLargeTransactionThreshold.HasValue)  s.FinanceLargeTransactionThreshold = request.FinanceLargeTransactionThreshold.Value;
        if (request.DocumentExpiryLeadDaysDefault.HasValue)     s.DocExpiryLeadDaysDefault = request.DocumentExpiryLeadDaysDefault.Value;
        if (request.DocumentExpiryLeadDaysIdentity.HasValue)    s.DocExpiryLeadDaysIdentity = request.DocumentExpiryLeadDaysIdentity.Value;
        if (request.DocumentExpiryLeadDaysMedical.HasValue)     s.DocExpiryLeadDaysMedical = request.DocumentExpiryLeadDaysMedical.Value;
        if (request.DocumentExpiryLeadDaysInsurance.HasValue)   s.DocExpiryLeadDaysInsurance = request.DocumentExpiryLeadDaysInsurance.Value;
        if (request.LateArrivalToleranceMinutes.HasValue)       s.LateArrivalToleranceMinutes = request.LateArrivalToleranceMinutes.Value;
        if (request.LocationStaleThresholdMinutes.HasValue)     s.LocationStaleThresholdMinutes = request.LocationStaleThresholdMinutes.Value;

        await _familyAdminConfigRepository.UpsertVaultFamilySettingsAsync(s, cancellationToken);
        await WriteAuditLogAsync(currentUserId, familyId, "AlertThresholdsUpdated", "VaultFamilySettings",
            familyId.ToString(), null, null, cancellationToken);

        return await GetAlertThresholdsAsync(currentUserId, familyId, cancellationToken);
    }

    public async Task<EmergencyAccessRulesDto> GetEmergencyAccessRulesAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);
        var contacts = DeserializeContacts(s.EmergencyContactsJson);

        return new EmergencyAccessRulesDto(
            s.EmergencyAccessMode.ToString(),
            s.EmergencyLinkExpiryHours,
            contacts);
    }

    public async Task<EmergencyAccessRulesDto> UpdateEmergencyAccessRulesAsync(
        Guid currentUserId, Guid familyId,
        UpdateEmergencyAccessRulesRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        if (request.AccessMode is not null &&
            Enum.TryParse<EmergencyAccessMode>(request.AccessMode, out var mode))
            s.EmergencyAccessMode = mode;

        if (request.EmergencyLinkExpiryHours.HasValue)
        {
            var expiry = request.EmergencyLinkExpiryHours.Value;
            if (expiry is < 1 or > 168)
                throw new ValidationException(new Dictionary<string, string[]>
                    { ["EmergencyLinkExpiryHours"] = ["Must be between 1 and 168 hours (7 days)."] });
            s.EmergencyLinkExpiryHours = expiry;
        }

        if (request.EmergencyContacts is not null)
        {
            if (request.EmergencyContacts.Count > 3)
                throw new ValidationException(new Dictionary<string, string[]>
                    { ["EmergencyContacts"] = ["Maximum 3 emergency contacts allowed."] });
            s.EmergencyContactsJson = JsonSerializer.Serialize(request.EmergencyContacts);
        }

        await _familyAdminConfigRepository.UpsertVaultFamilySettingsAsync(s, cancellationToken);
        await WriteAuditLogAsync(currentUserId, familyId, "EmergencyConfigUpdated", "VaultFamilySettings",
            familyId.ToString(), null, null, cancellationToken);

        return await GetEmergencyAccessRulesAsync(currentUserId, familyId, cancellationToken);
    }

    public async Task<FinancePrivacyConfigDto> GetFinancePrivacyConfigAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        return new FinancePrivacyConfigDto(
            s.DefaultAdultEarningMemberTier,
            s.DefaultIndependentMemberTier,
            s.ConsentReminderIntervalDays,
            s.AutoExcludeSalaryCredits);
    }

    public async Task<FinancePrivacyConfigDto> UpdateFinancePrivacyConfigAsync(
        Guid currentUserId, Guid familyId,
        UpdateFinancePrivacyConfigRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        var s = await GetOrCreateVaultSettingsAsync(familyId, cancellationToken);

        if (request.DefaultAdultEarningMemberTier.HasValue)
        {
            if (request.DefaultAdultEarningMemberTier.Value is < 1 or > 3)
                throw new ValidationException(new Dictionary<string, string[]>
                    { ["DefaultAdultEarningMemberTier"] = ["PrivacyTier must be 1, 2, or 3."] });
            s.DefaultAdultEarningMemberTier = request.DefaultAdultEarningMemberTier.Value;
        }
        if (request.DefaultIndependentMemberTier.HasValue)
        {
            if (request.DefaultIndependentMemberTier.Value is < 1 or > 3)
                throw new ValidationException(new Dictionary<string, string[]>
                    { ["DefaultIndependentMemberTier"] = ["PrivacyTier must be 1, 2, or 3."] });
            s.DefaultIndependentMemberTier = request.DefaultIndependentMemberTier.Value;
        }
        if (request.ConsentReminderIntervalDays.HasValue) s.ConsentReminderIntervalDays = request.ConsentReminderIntervalDays.Value;
        if (request.AutoExcludeSalaryCredits.HasValue)    s.AutoExcludeSalaryCredits = request.AutoExcludeSalaryCredits.Value;

        await _familyAdminConfigRepository.UpsertVaultFamilySettingsAsync(s, cancellationToken);
        await WriteAuditLogAsync(currentUserId, familyId, "FinancePrivacyConfigUpdated", "VaultFamilySettings",
            familyId.ToString(), null, null, cancellationToken);

        return await GetFinancePrivacyConfigAsync(currentUserId, familyId, cancellationToken);
    }

    // ── L2 private helpers ────────────────────────────────────────────────────

    private async Task<VaultFamilySettings> GetOrCreateVaultSettingsAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        var existing = await _familyAdminConfigRepository.GetVaultFamilySettingsAsync(familyId, cancellationToken);
        if (existing is not null) return existing;

        var created = new VaultFamilySettings { FamilyId = familyId };
        await _familyAdminConfigRepository.UpsertVaultFamilySettingsAsync(created, cancellationToken);
        return created;
    }

    private static IReadOnlyCollection<HybridRoutingRuleDto> DeserializeHybridRouting(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<HybridRoutingRuleDto>();
        try { return JsonSerializer.Deserialize<HybridRoutingRuleDto[]>(json) ?? Array.Empty<HybridRoutingRuleDto>(); }
        catch { return Array.Empty<HybridRoutingRuleDto>(); }
    }

    private static IReadOnlyCollection<EmergencyContactDto> DeserializeContacts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<EmergencyContactDto>();
        try { return JsonSerializer.Deserialize<EmergencyContactDto[]>(json) ?? Array.Empty<EmergencyContactDto>(); }
        catch { return Array.Empty<EmergencyContactDto>(); }
    }

    private static long ResolveQuotaBytes(Guid familyId)
    {
        // Plan-based quota — resolved from subscription in production.
        // Returns Premium default (10 GB) for MVP.
        return 10L * 1024 * 1024 * 1024;
    }

    private async Task WriteAuditLogAsync(
        Guid currentUserId,
        Guid familyId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        CancellationToken cancellationToken)
    {
        await _auditLogRepository.AddAsync(
            new AuditLog
            {
                UserId = currentUserId,
                FamilyId = familyId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues
            },
            cancellationToken);
    }
}
