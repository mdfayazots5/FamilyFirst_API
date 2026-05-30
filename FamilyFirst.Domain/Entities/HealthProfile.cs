using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class HealthProfile : BaseEntity
{
    public Guid FamilyId { get; set; }

    public Guid FamilyMemberId { get; set; }

    public string BloodGroup { get; set; } = string.Empty;

    public string? KnownAllergiesJson { get; set; }

    public string? ChronicConditionsJson { get; set; }

    public string? PrimaryDoctorName { get; set; }

    public string? PrimaryDoctorPhone { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactRelationship { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public bool OrganDonor { get; set; }

    public Family Family { get; set; } = null!;

    public FamilyMember FamilyMember { get; set; } = null!;

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();

    public ICollection<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();

    public ICollection<HeightWeightRecord> HeightWeightRecords { get; set; } = new List<HeightWeightRecord>();

    public ICollection<EmergencyCardLink> EmergencyCardLinks { get; set; } = new List<EmergencyCardLink>();
}
