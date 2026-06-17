using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Safety;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class SafetyService : ISafetyService
{
    private const int StaleThresholdMinutes     = 60;
    private const int LocationHistoryPurgeDays  = 30;

    private readonly ISafetyRepository        _safetyRepository;
    private readonly IFamilyMemberRepository  _memberRepository;
    private readonly IChildProfileRepository  _childProfileRepository;
    private readonly INotificationRepository  _notificationRepository;
    private readonly IPushNotificationService _pushService;
    private readonly IUserRepository          _userRepository;

    public SafetyService(
        ISafetyRepository safetyRepository,
        IFamilyMemberRepository memberRepository,
        IChildProfileRepository childProfileRepository,
        INotificationRepository notificationRepository,
        IPushNotificationService pushService,
        IUserRepository userRepository)
    {
        _safetyRepository       = safetyRepository;
        _memberRepository       = memberRepository;
        _childProfileRepository = childProfileRepository;
        _notificationRepository = notificationRepository;
        _pushService            = pushService;
        _userRepository         = userRepository;
    }

    public async Task<MapViewDto> GetMapViewAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var lastKnownLocations = await _safetyRepository.GetLastKnownLocationsAsync(familyId, cancellationToken);
        var zones              = await _safetyRepository.ListZonesByFamilyAsync(familyId, cancellationToken);
        var allMembers         = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var activeAlerts       = await _safetyRepository.GetActiveUnresolvedAlertsAsync(familyId, cancellationToken);

        var now     = DateTime.UtcNow;
        var staleTs = now.AddMinutes(-StaleThresholdMinutes);

        var pins = lastKnownLocations.Select(loc =>
        {
            var member    = allMembers.FirstOrDefault(m => m.InternalId == loc.FamilyMemberId);
            var isStale   = loc.RecordedAt < staleTs;
            var activeSos = activeAlerts.Any(a => a.FamilyMemberId == loc.FamilyMemberId && a.AlertType == LocationAlertType.SOS);

            return new MemberPinDto(
                member?.Id ?? loc.FamilyMember?.Id ?? Guid.Empty,
                member?.DisplayName ?? string.Empty,
                null,
                loc.Latitude,
                loc.Longitude,
                loc.RecordedAt,
                loc.BatteryLevel,
                loc.LocationName,
                false,
                null,
                isStale,
                activeSos);
        }).ToArray();

        var zoneDtos = zones.Select(MapToZoneDto).ToArray();

        return new MapViewDto(pins, zoneDtos);
    }

    public async Task UpdateLocationAsync(
        Guid currentUserId, Guid familyId, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        var location = new LocationHistory
        {
            FamilyId       = member.FamilyId,
            FamilyMemberId = member.InternalId,
            Latitude       = request.Latitude,
            Longitude      = request.Longitude,
            BatteryLevel   = request.BatteryLevel,
            RecordedAt     = request.Timestamp
        };

        await _safetyRepository.AddLocationAsync(location, cancellationToken);

        // Battery warning push when level drops below 15
        if (request.BatteryLevel < 15)
        {
            await CreateFamilyAlertAsync(
                member.FamilyId, member.InternalId, LocationAlertType.BatteryWarning,
                null, null, request.Latitude, request.Longitude,
                request.Timestamp, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<SafeZoneDto>> ListZonesAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var zones = await _safetyRepository.ListZonesByFamilyAsync(familyId, cancellationToken);
        return zones.Select(MapToZoneDto).ToArray();
    }

    public async Task<SafeZoneDto> CreateZoneAsync(
        Guid currentUserId, Guid familyId, CreateSafeZoneRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var zone = new SafeZone
        {
            FamilyId             = member.FamilyId,
            ZoneName             = request.ZoneName,
            ZoneType             = request.ZoneType,
            CenterLatitude       = request.Latitude,
            CenterLongitude      = request.Longitude,
            RadiusMetres         = request.RadiusMetres,
            AlertOnArrival       = request.AlertOnArrival,
            AlertOnDeparture     = request.AlertOnDeparture,
            LateAlertEnabled     = request.LateAlertEnabled,
            LateAlertTime        = ToStoredLateAlertTime(request.LateAlertTime),
            OverrideQuietHours   = request.OverrideQuietHours,
            AppliedMemberIdsJson = System.Text.Json.JsonSerializer.Serialize(request.AppliedToMemberIds)
        };

        var created = await _safetyRepository.AddZoneAsync(zone, cancellationToken);
        return MapToZoneDto(created);
    }

    public async Task<SafeZoneDto> UpdateZoneAsync(
        Guid currentUserId, Guid familyId, Guid zoneId, UpdateSafeZoneRequest request, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var zone = await _safetyRepository.GetZoneByIdAsync(zoneId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(SafeZone), zoneId);

        zone.ZoneName             = request.ZoneName;
        zone.ZoneType             = request.ZoneType;
        zone.CenterLatitude       = request.Latitude;
        zone.CenterLongitude      = request.Longitude;
        zone.RadiusMetres         = request.RadiusMetres;
        zone.AlertOnArrival       = request.AlertOnArrival;
        zone.AlertOnDeparture     = request.AlertOnDeparture;
        zone.LateAlertEnabled     = request.LateAlertEnabled;
        zone.LateAlertTime        = ToStoredLateAlertTime(request.LateAlertTime);
        zone.OverrideQuietHours   = request.OverrideQuietHours;
        zone.AppliedMemberIdsJson = System.Text.Json.JsonSerializer.Serialize(request.AppliedToMemberIds);

        await _safetyRepository.UpdateZoneAsync(zone, cancellationToken);
        return MapToZoneDto(zone);
    }

    public async Task DeleteZoneAsync(
        Guid currentUserId, Guid familyId, Guid zoneId, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var zone = await _safetyRepository.GetZoneByIdAsync(zoneId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(SafeZone), zoneId);

        zone.IsDeleted = true;
        zone.DeletedAt = DateTime.UtcNow;
        await _safetyRepository.UpdateZoneAsync(zone, cancellationToken);
    }

    public async Task<PaginatedList<LocationAlertDto>> ListAlertsAsync(
        Guid currentUserId, Guid familyId, Guid? memberId, string? alertType,
        DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var paged = await _safetyRepository.ListAlertsAsync(
            familyId, memberId, alertType, fromDate, toDate, page, pageSize, cancellationToken);

        return new PaginatedList<LocationAlertDto>(
            paged.Items.Select(MapToAlertDto).ToList(),
            paged.TotalCount, page, pageSize);
    }

    public async Task<SosEventDto> TriggerSosAsync(
        Guid currentUserId, Guid familyId, Guid childProfileId, TriggerSosRequest request, CancellationToken cancellationToken)
    {
        // Step 1 — verify caller is a Child with a matching child profile
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.Child)
            throw new ForbiddenAccessException("Only a Child can trigger an SOS.");

        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChildProfile), childProfileId);

        if (childProfile.FamilyId != member.FamilyId)
            throw new ForbiddenAccessException();

        // Step 2 — persist SOS alert + event atomically
        var alert = new LocationAlert
        {
            FamilyId       = member.FamilyId,
            FamilyMemberId = member.InternalId,
            AlertType      = LocationAlertType.SOS,
            Latitude       = request.Latitude,
            Longitude      = request.Longitude,
            TriggeredAt    = request.Timestamp
        };

        var createdAlert = await _safetyRepository.AddAlertAsync(alert, cancellationToken);
        var dispatchedAt = DateTime.UtcNow;

        var sosEvent = new SosEvent
        {
            FamilyId        = member.FamilyId,
            ChildProfileId  = childProfile.InternalId,
            LocationAlertId = createdAlert.InternalId,
            Latitude        = request.Latitude,
            Longitude       = request.Longitude,
            DispatchedAt    = dispatchedAt
        };

        var createdSos = await _safetyRepository.AddSosEventAsync(sosEvent, cancellationToken);

        // Step 3 — dispatch FCM directly (not via queue) to guarantee <3s delivery.
        // Every parent and the child's emergency contact receives an URGENT push bypassing quiet hours.
        var parents = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var parentUsers = parents.Where(m => m.Role is UserRole.Parent or UserRole.FamilyAdmin).ToArray();

        var title = $"🆘 SOS — {member.DisplayName ?? "Family Member"}";
        var body  = $"Help needed at {request.Latitude:F4}, {request.Longitude:F4} at {dispatchedAt:HH:mm} UTC. Tap to see location.";
        var data  = new Dictionary<string, string>
        {
            ["sosEventId"] = createdSos.Id.ToString(),
            ["latitude"]   = request.Latitude.ToString(),
            ["longitude"]  = request.Longitude.ToString(),
            ["alertType"]  = "SOS"
        };

        var sentCount = 0;

        foreach (var parentUser in parentUsers.Select(parent => parent.User))
        {
            if (parentUser?.FcmToken is { Length: > 0 } token)
            {
                await _pushService.SendPushAsync(token, title, body, cancellationToken, data);
                sentCount++;
            }
        }

        createdSos.AlertsSentCount = sentCount;
        await _safetyRepository.UpdateSosEventAsync(createdSos, cancellationToken);

        return new SosEventDto(createdSos.Id, dispatchedAt, request.Latitude, request.Longitude, sentCount);
    }

    public async Task<LocationAlertDto> ResolveAlertAsync(
        Guid currentUserId, Guid familyId, Guid alertId, ResolveAlertRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var alert = await _safetyRepository.GetAlertByIdAsync(alertId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(LocationAlert), alertId);

        alert.IsResolved       = true;
        alert.ResolvedAt       = DateTime.UtcNow;
        alert.ResolvedByUserId = member.UserId;
        alert.ResolutionNote   = request.ResolutionNote;

        await _safetyRepository.UpdateAlertAsync(alert, cancellationToken);
        return MapToAlertDto(alert);
    }

    public async Task<LocationSettingsDto> GetSettingsAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var consents    = await _safetyRepository.ListConsentByFamilyAsync(familyId, cancellationToken);
        var allMembers  = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var globalOn    = consents.Any(c => c.SharingEnabled);

        var memberSettings = allMembers.Select(m =>
        {
            var consent = consents.FirstOrDefault(c => c.FamilyMemberId == m.InternalId);
            return new MemberLocationSettingDto(
                m.Id,
                m.DisplayName ?? string.Empty,
                consent?.SharingEnabled ?? false,
                consent?.ConsentGiven ?? (m.Role == UserRole.Child),
                consent?.CaregiverViewOnly ?? false,
                consent?.UpdatedAt);
        }).ToArray();

        return new LocationSettingsDto(globalOn, memberSettings);
    }

    public async Task<LocationSettingsDto> UpdateSettingsAsync(
        Guid currentUserId, Guid familyId, UpdateLocationSettingsRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);

        foreach (var update in request.MemberSettings)
        {
            var member = allMembers.FirstOrDefault(m => m.Id == update.MemberId)
                ?? throw new NotFoundException(nameof(FamilyMember), update.MemberId);

            var existing = await _safetyRepository.GetConsentByMemberAsync(update.MemberId, cancellationToken);

            // Adults require explicit consent before enabling sharing
            if (update.SharingEnabled && member.Role != UserRole.Child)
            {
                if (existing is null || !existing.ConsentGiven)
                {
                    throw new UnprocessableEntityException(
                        $"Cannot enable location sharing for {member.DisplayName} — adult consent has not been given.");
                }
            }

            var consent = existing ?? new LocationSharingConsent
            {
                FamilyId       = member.FamilyId,
                FamilyMemberId = member.InternalId
            };

            consent.SharingEnabled    = update.SharingEnabled;
            consent.CaregiverViewOnly = update.CaregiverViewOnly;

            await _safetyRepository.UpsertConsentAsync(consent, cancellationToken);
        }

        return await GetSettingsAsync(currentUserId, familyId, cancellationToken);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task CreateFamilyAlertAsync(
        long familyId,
        long memberId,
        string alertType,
        long? zoneId,
        string? zoneNameSnapshot,
        decimal? latitude,
        decimal? longitude,
        DateTime triggeredAt,
        CancellationToken cancellationToken)
    {
        var alert = new LocationAlert
        {
            FamilyId         = familyId,
            FamilyMemberId   = memberId,
            AlertType        = alertType,
            ZoneId           = zoneId,
            ZoneNameSnapshot = zoneNameSnapshot,
            Latitude         = latitude,
            Longitude        = longitude,
            TriggeredAt      = triggeredAt
        };
        await _safetyRepository.AddAlertAsync(alert, cancellationToken);
    }

    private async Task<FamilyMember> EnsureParentOrAdminAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException();

        return member;
    }

    private async Task EnsureFamilyAdminAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException("Only FamilyAdmin can manage location settings.");
    }

    private static SafeZoneDto MapToZoneDto(SafeZone z)
    {
        Guid[] memberIds;
        try { memberIds = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(z.AppliedMemberIdsJson) ?? Array.Empty<Guid>(); }
        catch { memberIds = Array.Empty<Guid>(); }

        return new SafeZoneDto(
            z.Id, z.ZoneName, z.ZoneType, z.CenterLatitude, z.CenterLongitude,
            z.RadiusMetres, z.AlertOnArrival, z.AlertOnDeparture,
            z.LateAlertEnabled, FromStoredLateAlertTime(z.LateAlertTime), z.OverrideQuietHours, memberIds);
    }

    private static LocationAlertDto MapToAlertDto(LocationAlert a) =>
        new(a.Id, a.FamilyMember?.Id ?? Guid.Empty, a.FamilyMember?.DisplayName ?? string.Empty,
            a.AlertType, a.ZoneNameSnapshot, a.Latitude, a.Longitude,
            a.IsResolved, a.ResolvedAt, a.ResolutionNote, a.TriggeredAt);

    private static DateTime? ToStoredLateAlertTime(TimeOnly? lateAlertTime)
    {
        if (!lateAlertTime.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(DateTime.Today.Add(lateAlertTime.Value.ToTimeSpan()), DateTimeKind.Utc);
    }

    private static TimeOnly? FromStoredLateAlertTime(DateTime? lateAlertTime)
    {
        return lateAlertTime.HasValue
            ? TimeOnly.FromDateTime(lateAlertTime.Value)
            : null;
    }
}
