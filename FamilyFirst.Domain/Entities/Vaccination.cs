using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Vaccination : BaseEntity
{
    public long HealthProfileId { get; set; }

    public long FamilyId { get; set; }

    public string VaccineName { get; set; } = string.Empty;

    public string Status { get; set; } = Enums.VaccinationStatus.Due;

    public DateTime? GivenDate { get; set; }

    public DateTime? DueDate { get; set; }

    public long? LinkedVaultDocumentId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public VaultDocument? LinkedVaultDocument { get; set; }
}
