namespace FamilyFirst.Application.DTOs.Medical;

public sealed record HealthProfileSummaryDto(
    Guid MemberId,
    string MemberName,
    string BloodGroup,
    bool HasAllergies,
    int ActiveMedicationCount,
    DateTime? NextVaccinationDue,
    bool IsProfileComplete
);
