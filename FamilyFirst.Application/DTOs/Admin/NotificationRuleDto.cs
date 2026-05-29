using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed record NotificationRuleDto(
    Guid RuleId,
    Guid FamilyId,
    string RuleKey,
    bool IsEnabled,
    NotificationPriority? PriorityOverride,
    int? DeliveryDelayMinutes,
    DateTime UpdatedAt);
