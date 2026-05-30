namespace FamilyFirst.Application.DTOs.Safety;

public sealed record SafeZoneDto(
    Guid ZoneId,
    string ZoneName,
    string ZoneType,
    decimal CenterLatitude,
    decimal CenterLongitude,
    int RadiusMetres,
    bool AlertOnArrival,
    bool AlertOnDeparture,
    bool LateAlertEnabled,
    TimeOnly? LateAlertTime,
    bool OverrideQuietHours,
    Guid[] AppliedToMemberIds
);

public sealed record CreateSafeZoneRequest(
    string ZoneName,
    string ZoneType,
    decimal Latitude,
    decimal Longitude,
    int RadiusMetres,
    Guid[] AppliedToMemberIds,
    bool AlertOnArrival,
    bool AlertOnDeparture,
    bool LateAlertEnabled,
    TimeOnly? LateAlertTime,
    bool OverrideQuietHours
);

public sealed record UpdateSafeZoneRequest(
    string ZoneName,
    string ZoneType,
    decimal Latitude,
    decimal Longitude,
    int RadiusMetres,
    Guid[] AppliedToMemberIds,
    bool AlertOnArrival,
    bool AlertOnDeparture,
    bool LateAlertEnabled,
    TimeOnly? LateAlertTime,
    bool OverrideQuietHours
);
