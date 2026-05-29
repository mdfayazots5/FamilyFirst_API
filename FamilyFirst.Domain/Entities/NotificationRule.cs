using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class NotificationRule
{
    public Guid RuleId { get; set; } = Guid.NewGuid();

    public Guid FamilyId { get; set; }

    public string RuleKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public NotificationPriority? PriorityOverride { get; set; }

    public int? DeliveryDelayMinutes { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
