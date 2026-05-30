using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Safety;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ISafetyService
{
    Task<MapViewDto> GetMapViewAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task UpdateLocationAsync(Guid currentUserId, Guid familyId, UpdateLocationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SafeZoneDto>> ListZonesAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<SafeZoneDto> CreateZoneAsync(Guid currentUserId, Guid familyId, CreateSafeZoneRequest request, CancellationToken cancellationToken);

    Task<SafeZoneDto> UpdateZoneAsync(Guid currentUserId, Guid familyId, Guid zoneId, UpdateSafeZoneRequest request, CancellationToken cancellationToken);

    Task DeleteZoneAsync(Guid currentUserId, Guid familyId, Guid zoneId, CancellationToken cancellationToken);

    Task<PaginatedList<LocationAlertDto>> ListAlertsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid? memberId,
        string? alertType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<SosEventDto> TriggerSosAsync(Guid currentUserId, Guid familyId, Guid childProfileId, TriggerSosRequest request, CancellationToken cancellationToken);

    Task<LocationAlertDto> ResolveAlertAsync(Guid currentUserId, Guid familyId, Guid alertId, ResolveAlertRequest request, CancellationToken cancellationToken);

    Task<LocationSettingsDto> GetSettingsAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<LocationSettingsDto> UpdateSettingsAsync(Guid currentUserId, Guid familyId, UpdateLocationSettingsRequest request, CancellationToken cancellationToken);
}
