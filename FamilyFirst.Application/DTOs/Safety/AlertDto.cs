namespace FamilyFirst.Application.DTOs.Safety;

public sealed record LocationAlertDto(
    Guid AlertId,
    Guid MemberId,
    string MemberName,
    string AlertType,
    string? ZoneName,
    decimal? Latitude,
    decimal? Longitude,
    bool IsResolved,
    DateTime? ResolvedAt,
    string? ResolutionNote,
    DateTime TriggeredAt
);

public sealed record SosEventDto(
    Guid SosEventId,
    DateTime DispatchedAt,
    decimal Latitude,
    decimal Longitude,
    int AlertsSentCount
);

public sealed record TriggerSosRequest(
    decimal Latitude,
    decimal Longitude,
    DateTime Timestamp
);

public sealed record ResolveAlertRequest(
    string? ResolutionNote
);
