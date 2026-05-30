namespace FamilyFirst.Application.DTOs.Safety;

public sealed record UpdateLocationRequest(
    decimal Latitude,
    decimal Longitude,
    int BatteryLevel,
    DateTime Timestamp
);

public sealed record MapViewDto(
    IReadOnlyCollection<MemberPinDto> MemberPins,
    IReadOnlyCollection<SafeZoneDto> SafeZones
);

public sealed record MemberPinDto(
    Guid MemberId,
    string MemberName,
    string? PhotoUrl,
    decimal? LastKnownLat,
    decimal? LastKnownLng,
    DateTime? LastUpdatedAt,
    int? BatteryLevel,
    string? CurrentLocationName,
    bool IsInsideZone,
    string? ZoneType,
    bool IsStale,
    bool HasActiveSos
);

public sealed record LocationSettingsDto(
    bool GlobalSharingEnabled,
    IReadOnlyCollection<MemberLocationSettingDto> MemberSettings
);

public sealed record MemberLocationSettingDto(
    Guid MemberId,
    string MemberName,
    bool SharingEnabled,
    bool ConsentGiven,
    bool CaregiverViewOnly,
    DateTime? LastUpdatedAt
);

public sealed record UpdateLocationSettingsRequest(
    bool? GlobalSharingEnabled,
    IReadOnlyCollection<UpdateMemberLocationSettingDto> MemberSettings
);

public sealed record UpdateMemberLocationSettingDto(
    Guid MemberId,
    bool SharingEnabled,
    bool CaregiverViewOnly
);
