using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class VaultExpiryReminderLog : BaseEntity
{
    public long VaultDocumentId { get; set; }

    public Guid DocumentId
    {
        get => VaultDocument?.Id ?? Guid.Empty;
        set { }
    }

    public long FamilyId { get; set; }

    public int ThresholdDays { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public VaultDocument VaultDocument { get; set; } = null!;

    public Family Family { get; set; } = null!;
}
