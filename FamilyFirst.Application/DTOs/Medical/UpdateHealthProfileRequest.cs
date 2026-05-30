namespace FamilyFirst.Application.DTOs.Medical;

public sealed record UpdateHealthProfileRequest(
    string BloodGroup,
    IReadOnlyCollection<AllergyInput>? KnownAllergies,
    IReadOnlyCollection<string>? ChronicConditions,
    string? PrimaryDoctorName,
    string? PrimaryDoctorPhone,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    bool? OrganDonor
);

public sealed record AllergyInput(string Text, string Category);
