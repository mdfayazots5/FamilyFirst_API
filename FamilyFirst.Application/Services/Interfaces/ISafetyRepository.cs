using FamilyFirst.Application.Common.Models;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ISafetyRepository
{
    // Safe Zones
    Task<IReadOnlyCollection<SafeZone>> ListZonesByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<SafeZone?> GetZoneByIdAsync(Guid zoneId, Guid familyId, CancellationToken cancellationToken);

    Task<SafeZone> AddZoneAsync(SafeZone zone, CancellationToken cancellationToken);

    Task UpdateZoneAsync(SafeZone zone, CancellationToken cancellationToken);

    // Location History
    Task<LocationHistory?> GetLastKnownLocationAsync(Guid familyMemberId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LocationHistory>> GetLastKnownLocationsAsync(Guid familyId, CancellationToken cancellationToken);

    Task AddLocationAsync(LocationHistory location, CancellationToken cancellationToken);

    Task PurgeOldLocationHistoryAsync(DateTime olderThan, CancellationToken cancellationToken);

    // Location Alerts
    Task<PaginatedList<LocationAlert>> ListAlertsAsync(
        Guid familyId,
        Guid? memberId,
        string? alertType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<LocationAlert?> GetAlertByIdAsync(Guid alertId, Guid familyId, CancellationToken cancellationToken);

    Task<LocationAlert> AddAlertAsync(LocationAlert alert, CancellationToken cancellationToken);

    Task UpdateAlertAsync(LocationAlert alert, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LocationAlert>> GetActiveUnresolvedAlertsAsync(Guid familyId, CancellationToken cancellationToken);

    // SOS Events
    Task<SosEvent> AddSosEventAsync(SosEvent sosEvent, CancellationToken cancellationToken);

    Task UpdateSosEventAsync(SosEvent sosEvent, CancellationToken cancellationToken);

    // Consent
    Task<IReadOnlyCollection<LocationSharingConsent>> ListConsentByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<LocationSharingConsent?> GetConsentByMemberAsync(Guid familyMemberId, CancellationToken cancellationToken);

    Task UpsertConsentAsync(LocationSharingConsent consent, CancellationToken cancellationToken);

    // Late alert worker
    Task<IReadOnlyCollection<SafeZone>> GetZonesWithLateAlertDueAsync(TimeOnly currentTime, CancellationToken cancellationToken);

    Task<bool> ArrivalAlertExistsTodayAsync(Guid familyMemberId, Guid zoneId, CancellationToken cancellationToken);

    Task<bool> LateAlertAlreadySentTodayAsync(Guid familyMemberId, Guid zoneId, CancellationToken cancellationToken);
}
