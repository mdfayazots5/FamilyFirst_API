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

    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public SafetyService(
        ISafetyRepository safetyRepository,
        IFamilyMemberRepository memberRepository,
        IChildProfileRepository childProfileRepository,
        INotificationRepository notificationRepository,
        IPushNotificationService pushService,
        IUserRepository userRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
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
        var consents           = await _safetyRepository.ListConsentByFamilyAsync(familyId, cancellationToken);

        var now     = DateTime.UtcNow;
        var staleTs = now.AddMinutes(-StaleThresholdMinutes);

        // Members who stopped sharing (or never consented) are not shown, even with a stored last location.
        var sharingMemberIds = allMembers
            .Where(m => IsSharingAllowed(m, consents.FirstOrDefault(c => c.FamilyMemberId == m.InternalId)))
            .Select(m => m.InternalId)
            .ToHashSet();

        var pins = lastKnownLocations.Where(loc => sharingMemberIds.Contains(loc.FamilyMemberId)).Select(loc =>
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

        return LogResult(nameof(GetMapViewAsync), familyId, new MapViewDto(pins, zoneDtos));
    }

    public async Task UpdateLocationAsync(
        Guid currentUserId, Guid familyId, UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        // §14.4 rule 11: never store a location the member is not sharing. Silent no-op keeps the client's
        // periodic sender contract unchanged (it cannot act on an error).
        var consent = await _safetyRepository.GetConsentByMemberAsync(member.Id, cancellationToken);
        if (!IsSharingAllowed(member, consent))
        {
            LogCompletion(nameof(UpdateLocationAsync), familyId, hasResult: false);
            return;
        }

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
        LogCompletion(nameof(UpdateLocationAsync), familyId);
    }

    public async Task<IReadOnlyCollection<SafeZoneDto>> ListZonesAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var zones = await _safetyRepository.ListZonesByFamilyAsync(familyId, cancellationToken);
        return LogResult(nameof(ListZonesAsync), familyId, zones.Select(MapToZoneDto).ToArray());
    }

    public async Task<SafeZoneDto> CreateZoneAsync(
        Guid currentUserId, Guid familyId, CreateSafeZoneRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        foreach (var memberId in request.AppliedToMemberIds ?? [])
            await ResolveMemberIdAsync(memberId, member.FamilyId, cancellationToken);

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
        return LogResult(nameof(CreateZoneAsync), familyId, MapToZoneDto(created));
    }

    public async Task<SafeZoneDto> UpdateZoneAsync(
        Guid currentUserId, Guid familyId, Guid zoneId, UpdateSafeZoneRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var zone = await _safetyRepository.GetZoneByIdAsync(zoneId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        foreach (var memberId in request.AppliedToMemberIds ?? [])
            await ResolveMemberIdAsync(memberId, member.FamilyId, cancellationToken);

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
        return LogResult(nameof(UpdateZoneAsync), familyId, MapToZoneDto(zone));
    }

    public async Task DeleteZoneAsync(
        Guid currentUserId, Guid familyId, Guid zoneId, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);

        var zone = await _safetyRepository.GetZoneByIdAsync(zoneId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        zone.IsDeleted = true;
        zone.DeletedAt = DateTime.UtcNow;
        await _safetyRepository.UpdateZoneAsync(zone, cancellationToken);
        LogCompletion(nameof(DeleteZoneAsync), familyId);
    }

    public async Task<PaginatedList<LocationAlertDto>> ListAlertsAsync(
        Guid currentUserId, Guid familyId, Guid? memberId, string? alertType,
        DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var paged = await _safetyRepository.ListAlertsAsync(
            familyId, memberId, alertType, fromDate, toDate, page, pageSize, cancellationToken);

        return LogResult(nameof(ListAlertsAsync), familyId, new PaginatedList<LocationAlertDto>(
            paged.Items.Select(MapToAlertDto).ToList(),
            paged.TotalCount, page, pageSize));
    }

    public async Task<SosEventDto> TriggerSosAsync(
        Guid currentUserId, Guid familyId, Guid childProfileId, TriggerSosRequest request, CancellationToken cancellationToken)
    {
        // Step 1 — verify caller is a Child with a matching child profile
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.Child)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile, childProfileId.ToString(), member.FamilyId, cancellationToken);
        var childProfile = await _childProfileRepository.GetByIdForFamilyAsync(childProfileId, member.FamilyId, cancellationToken);
        if (resolvedChildId is null || childProfile is null)
        {
            // Always 404: a 403 here would reveal that a child profile exists in another family.
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        if (childProfile.FamilyMemberId != member.InternalId)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

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

        var dispatchedAt = DateTime.UtcNow;

        var sosEvent = new SosEvent
        {
            FamilyId        = member.FamilyId,
            ChildProfileId  = resolvedChildId.Value,
            Latitude        = request.Latitude,
            Longitude       = request.Longitude,
            DispatchedAt    = dispatchedAt
        };

        var createdSos = await _safetyRepository.AddSosAlertWithEventAsync(alert, sosEvent, cancellationToken);

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

        var sentCount = await SendSosPushesAsync(parentUsers, title, body, data, cancellationToken);

        createdSos.AlertsSentCount = sentCount;
        await _safetyRepository.UpdateSosEventAsync(createdSos, cancellationToken);

        return LogResult(nameof(TriggerSosAsync), familyId, new SosEventDto(createdSos.Id, dispatchedAt, request.Latitude, request.Longitude, sentCount));
    }

    public async Task<LocationAlertDto> ResolveAlertAsync(
        Guid currentUserId, Guid familyId, Guid alertId, ResolveAlertRequest request, CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.ApproveReject, cancellationToken);

        var alert = await _safetyRepository.GetAlertByIdAsync(alertId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        alert.IsResolved       = true;
        alert.ResolvedAt       = DateTime.UtcNow;
        alert.ResolvedByUserId = member.UserId;
        alert.ResolutionNote   = request.ResolutionNote;

        await _safetyRepository.UpdateAlertAsync(alert, cancellationToken);
        return LogResult(nameof(ResolveAlertAsync), familyId, MapToAlertDto(alert));
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

        return LogResult(nameof(GetSettingsAsync), familyId, new LocationSettingsDto(globalOn, memberSettings));
    }

    public async Task<LocationSettingsDto> UpdateSettingsAsync(
        Guid currentUserId, Guid familyId, UpdateLocationSettingsRequest request, CancellationToken cancellationToken)
    {
        var caller = await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(caller.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var allMembers = await _memberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);

        foreach (var update in request.MemberSettings)
        {
            var memberId = await ResolveMemberIdAsync(update.MemberId, caller.FamilyId, cancellationToken);
            var member = allMembers.FirstOrDefault(m => m.Id == update.MemberId)
                ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

            var existing = await _safetyRepository.GetConsentByMemberAsync(update.MemberId, cancellationToken);

            // Adults require explicit consent before enabling sharing
            if (update.SharingEnabled && member.Role != UserRole.Child)
            {
                if (existing is null || !existing.ConsentGiven)
                {
                    throw new UnprocessableEntityException(await GetMessageAsync(FamilyFirstErrorCode.Consent_Required, cancellationToken));
                }
            }

            var consent = existing ?? new LocationSharingConsent
            {
                FamilyId       = member.FamilyId,
                FamilyMemberId = memberId
            };

            consent.SharingEnabled    = update.SharingEnabled;
            consent.CaregiverViewOnly = update.CaregiverViewOnly;

            await _safetyRepository.UpsertConsentAsync(consent, cancellationToken);
        }

        return LogResult(nameof(UpdateSettingsAsync), familyId, await GetSettingsAsync(currentUserId, familyId, cancellationToken));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static readonly TimeSpan SosPushTimeout = TimeSpan.FromSeconds(5);

    // Parallel with an overall timeout so one slow recipient cannot delay the rest; counts confirmed sends only.
    private async Task<int> SendSosPushesAsync(
        IEnumerable<FamilyMember> recipients,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SosPushTimeout);

        var sends = recipients
            .Select(recipient => recipient.User?.FcmToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct()
            .Select(token => TrySendPushAsync(token!, title, body, data, timeout.Token));

        var results = await Task.WhenAll(sends);
        return results.Count(sent => sent);
    }

    private async Task<bool> TrySendPushAsync(
        string token, string title, string body, IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken)
    {
        try
        {
            return await _pushService.SendPushAsync(token, title, body, cancellationToken, data);
        }
        catch (Exception)
        {
            // A failed or timed-out recipient must not fail the SOS; it is simply not counted as sent.
            return false;
        }
    }

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
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.View, cancellationToken);
        return member;
    }

    /// <summary>
    /// §14.4 rule 11: adults must explicitly opt in (ConsentGiven + SharingEnabled). For children the FamilyAdmin
    /// controls sharing (SharingEnabled). No consent row means sharing is off — matching GetSettingsAsync.
    /// </summary>
    private static bool IsSharingAllowed(FamilyMember member, LocationSharingConsent? consent) =>
        consent is not null
        && consent.SharingEnabled
        && (member.Role == UserRole.Child || consent.ConsentGiven);

    private async Task<FamilyMember> EnsureFamilyAdminAsync(
        Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.View, cancellationToken);
        return member;
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

    private Task<string> GetMessageAsync(FamilyFirstErrorCode code, CancellationToken cancellationToken) =>
        _errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CheckAsync(role, FamilyFirstModule.Safety, permission, cancellationToken))
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private T LogResult<T>(string method, Guid? familyId, T result)
    {
        LogCompletion(method, familyId, result is not null);
        return result;
    }

    private void LogCompletion(string method, Guid? familyId, bool hasResult = true) =>
        _apiLogService.Log(method, System.Text.Json.JsonSerializer.Serialize(new { FamilyId = familyId }),
            System.Text.Json.JsonSerializer.Serialize(new { HasResult = hasResult }));

    private async Task<long> ResolveMemberIdAsync(Guid memberId, long familyId, CancellationToken cancellationToken)
    {
        var id = await _masterDataResolver.ResolveAsync(MasterDataCodes.FamilyMember, memberId.ToString(), familyId, cancellationToken);
        return id ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }
}
