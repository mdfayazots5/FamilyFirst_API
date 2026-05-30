namespace FamilyFirst.Application.DTOs.Medical;

public sealed record PrescriptionDto(
    Guid PrescriptionId,
    string MedicationName,
    string Dosage,
    string Frequency,
    string PrescribingDoctor,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsRecurring,
    bool IsArchived,
    Guid? LinkedDocumentId
);

public sealed record AddPrescriptionRequest(
    string MedicationName,
    string Dosage,
    string Frequency,
    string PrescribingDoctor,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsRecurring,
    Guid? LinkedDocumentId
);
