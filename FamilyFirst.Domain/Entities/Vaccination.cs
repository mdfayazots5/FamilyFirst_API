using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Vaccination : BaseEntity
{
    public Guid HealthProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public string VaccineName { get; set; } = string.Empty;

    public string Status { get; set; } = Enums.VaccinationStatus.Due;

    public DateOnly? GivenDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public Guid? LinkedDocumentId { get; set; }

    public HealthProfile HealthProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;

    public VaultDocument? LinkedDocument { get; set; }
}
