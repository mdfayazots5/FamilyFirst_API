namespace FamilyFirst.Application.DTOs.Medical;

public sealed record VaccinationDto(
    Guid VaccinationId,
    string VaccineName,
    string Status,
    DateOnly? GivenDate,
    DateOnly? DueDate,
    Guid? LinkedDocumentId
);

public sealed record AddVaccinationRequest(
    string VaccineName,
    string Status,
    DateOnly? GivenDate,
    DateOnly? DueDate,
    Guid? LinkedDocumentId
);

public sealed record UpdateVaccinationStatusRequest(
    string Status,
    DateOnly? GivenDate
);
