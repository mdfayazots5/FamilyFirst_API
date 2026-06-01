using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class HealthRecord : BaseEntity
{
    public long HealthProfileId { get; set; }

    public long FamilyId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public long? LinkedVaultDocumentId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public VaultDocument? LinkedVaultDocument { get; set; }
}
