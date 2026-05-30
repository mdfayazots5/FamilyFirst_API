namespace FamilyFirst.Application.DTOs.Medical;

public sealed record HealthProfileDto(
    Guid HealthProfileId,
    Guid MemberId,
    string MemberName,
    string BloodGroup,
    IReadOnlyCollection<AllergyDto> KnownAllergies,
    IReadOnlyCollection<string> ChronicConditions,
    DoctorDto? PrimaryDoctor,
    ContactDto? EmergencyContact,
    bool OrganDonor,
    IReadOnlyCollection<PrescriptionDto> CurrentMedications,
    IReadOnlyCollection<VaccinationDto> VaccinationStatus,
    bool IsProfileComplete,
    DateTime LastUpdated
);

public sealed record AllergyDto(string Text, string Category);

public sealed record DoctorDto(string Name, string? Phone);

public sealed record ContactDto(string Name, string? Relationship, string? Phone);
