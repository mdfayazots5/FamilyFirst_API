using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Prescription : BaseEntity
{
    public long HealthProfileId { get; set; }

    public long FamilyId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public string PrescribingDoctor { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsRecurring { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public long? LinkedVaultDocumentId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public VaultDocument? LinkedVaultDocument { get; set; }
}
