using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class HealthRecord : BaseEntity
{
    public Guid HealthProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Guid? LinkedDocumentId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public VaultDocument? LinkedDocument { get; set; }
}
