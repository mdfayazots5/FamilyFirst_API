using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class NotificationRule : BaseEntity
{
    public long FamilyId { get; set; }

    public string RuleKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public NotificationPriority? PriorityOverride { get; set; }

    public int? DeliveryDelayMinutes { get; set; }

    public Family? Family { get; set; }
}
