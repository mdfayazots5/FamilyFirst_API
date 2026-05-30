using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultExpiryReminderLog : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Guid FamilyId { get; set; }

    public int ThresholdDays { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public VaultDocument Document { get; set; } = null!;

    public Family Family { get; set; } = null!;
}
