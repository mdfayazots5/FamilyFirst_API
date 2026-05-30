namespace FamilyFirst.Application.DTOs.Medical;

public sealed record EmergencyCardDto(
    Guid MemberId,
    string MemberName,
    string? MemberPhotoUrl,
    int? AgeYears,
    string BloodGroup,
    IReadOnlyCollection<AllergyDto> KnownAllergies,
    IReadOnlyCollection<ActiveMedicationDto> CurrentMedications,
    string? PrimaryDoctorName,
    string? PrimaryDoctorPhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool OrganDonor,
    bool IsProfileComplete
);

public sealed record ActiveMedicationDto(string MedicationName, string Dosage);

public sealed record ShareEmergencyCardRequest(
    int? ExpiryHours,
    string? Language
);

public sealed record EmergencyCardShareDto(
    string ShareLink,
    string QrCodeData,
    string? ShareableImageUrl,
    DateTime ExpiresAt
);
