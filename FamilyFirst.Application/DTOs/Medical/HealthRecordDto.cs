namespace FamilyFirst.Application.DTOs.Medical;

public sealed record HealthRecordDto(
    Guid HealthRecordId,
    string EventType,
    DateOnly EventDate,
    string Title,
    string? Notes,
    Guid? LinkedDocumentId
);

public sealed record AddHealthRecordRequest(
    string EventType,
    DateOnly EventDate,
    string Title,
    string? Notes,
    Guid? LinkedDocumentId
);
