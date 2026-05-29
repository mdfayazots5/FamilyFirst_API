using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Admin;

public sealed class UpdateNotificationRuleRequest
{
    public bool IsEnabled { get; init; } = true;

    public NotificationPriority? PriorityOverride { get; init; }

    public int? DeliveryDelayMinutes { get; init; }
}
