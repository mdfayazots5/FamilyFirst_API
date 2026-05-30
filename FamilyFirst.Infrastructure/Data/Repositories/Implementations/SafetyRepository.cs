using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class SafetyRepository : ISafetyRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public SafetyRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ── Safe Zones ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<SafeZone>> ListZonesByFamilyAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<SafeZone>()
            .Where(z => z.FamilyId == familyId)
            .OrderBy(z => z.ZoneName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SafeZone?> GetZoneByIdAsync(Guid zoneId, Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<SafeZone>()
            .SingleOrDefaultAsync(z => z.Id == zoneId && z.FamilyId == familyId, cancellationToken);
    }

    public async Task<SafeZone> AddZoneAsync(SafeZone zone, CancellationToken cancellationToken)
    {
        _dbContext.Set<SafeZone>().Add(zone);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return zone;
    }

    public async Task UpdateZoneAsync(SafeZone zone, CancellationToken cancellationToken)
    {
        _dbContext.Set<SafeZone>().Update(zone);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Location History ───────────────────────────────────────────────────

    public Task<LocationHistory?> GetLastKnownLocationAsync(Guid familyMemberId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<LocationHistory>()
            .Where(l => l.FamilyMemberId == familyMemberId)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LocationHistory>> GetLastKnownLocationsAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        // One row per member — latest RecordedAt
        var memberIds = await _dbContext.Set<LocationHistory>()
            .Where(l => l.FamilyId == familyId)
            .Select(l => l.FamilyMemberId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var results = new List<LocationHistory>(memberIds.Length);
        foreach (var memberId in memberIds)
        {
            var latest = await _dbContext.Set<LocationHistory>()
                .Include(l => l.FamilyMember)
                .Where(l => l.FamilyMemberId == memberId)
                .OrderByDescending(l => l.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null) results.Add(latest);
        }
        return results;
    }

    public async Task AddLocationAsync(LocationHistory location, CancellationToken cancellationToken)
    {
        _dbContext.Set<LocationHistory>().Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PurgeOldLocationHistoryAsync(DateTime olderThan, CancellationToken cancellationToken)
    {
        var old = await _dbContext.Set<LocationHistory>()
            .Where(l => l.RecordedAt < olderThan)
            .ToArrayAsync(cancellationToken);

        _dbContext.Set<LocationHistory>().RemoveRange(old);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Location Alerts ────────────────────────────────────────────────────

    public async Task<PaginatedList<LocationAlert>> ListAlertsAsync(
        Guid familyId,
        Guid? memberId,
        string? alertType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<LocationAlert>()
            .Include(a => a.FamilyMember)
            .Where(a => a.FamilyId == familyId);

        if (memberId.HasValue) query = query.Where(a => a.FamilyMemberId == memberId.Value);
        if (!string.IsNullOrWhiteSpace(alertType)) query = query.Where(a => a.AlertType == alertType);
        if (fromDate.HasValue) query = query.Where(a => a.TriggeredAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.TriggeredAt <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PaginatedList<LocationAlert>(items, totalCount, page, pageSize);
    }

    public Task<LocationAlert?> GetAlertByIdAsync(Guid alertId, Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<LocationAlert>()
            .SingleOrDefaultAsync(a => a.Id == alertId && a.FamilyId == familyId, cancellationToken);
    }

    public async Task<LocationAlert> AddAlertAsync(LocationAlert alert, CancellationToken cancellationToken)
    {
        _dbContext.Set<LocationAlert>().Add(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task UpdateAlertAsync(LocationAlert alert, CancellationToken cancellationToken)
    {
        _dbContext.Set<LocationAlert>().Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LocationAlert>> GetActiveUnresolvedAlertsAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<LocationAlert>()
            .Where(a => a.FamilyId == familyId && !a.IsResolved)
            .OrderByDescending(a => a.TriggeredAt)
            .ToArrayAsync(cancellationToken);
    }

    // ── SOS Events ─────────────────────────────────────────────────────────

    public async Task<SosEvent> AddSosEventAsync(SosEvent sosEvent, CancellationToken cancellationToken)
    {
        _dbContext.Set<SosEvent>().Add(sosEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return sosEvent;
    }

    public async Task UpdateSosEventAsync(SosEvent sosEvent, CancellationToken cancellationToken)
    {
        _dbContext.Set<SosEvent>().Update(sosEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Consent ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<LocationSharingConsent>> ListConsentByFamilyAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<LocationSharingConsent>()
            .Where(c => c.FamilyId == familyId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<LocationSharingConsent?> GetConsentByMemberAsync(Guid familyMemberId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<LocationSharingConsent>()
            .SingleOrDefaultAsync(c => c.FamilyMemberId == familyMemberId, cancellationToken);
    }

    public async Task UpsertConsentAsync(LocationSharingConsent consent, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Set<LocationSharingConsent>()
            .SingleOrDefaultAsync(c => c.FamilyMemberId == consent.FamilyMemberId, cancellationToken);

        if (existing is null)
            _dbContext.Set<LocationSharingConsent>().Add(consent);
        else
        {
            existing.SharingEnabled    = consent.SharingEnabled;
            existing.CaregiverViewOnly = consent.CaregiverViewOnly;
            _dbContext.Set<LocationSharingConsent>().Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Late alert worker ──────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<SafeZone>> GetZonesWithLateAlertDueAsync(
        TimeOnly currentTime, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<SafeZone>()
            .Where(z => z.LateAlertEnabled && z.LateAlertTime.HasValue && z.LateAlertTime.Value == currentTime)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ArrivalAlertExistsTodayAsync(
        Guid familyMemberId, Guid zoneId, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        return await _dbContext.Set<LocationAlert>()
            .AnyAsync(a =>
                a.FamilyMemberId == familyMemberId &&
                a.ZoneId == zoneId &&
                a.AlertType == LocationAlertType.ZoneArrival &&
                a.TriggeredAt >= todayStart,
                cancellationToken);
    }

    public async Task<bool> LateAlertAlreadySentTodayAsync(
        Guid familyMemberId, Guid zoneId, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        return await _dbContext.Set<LocationAlert>()
            .AnyAsync(a =>
                a.FamilyMemberId == familyMemberId &&
                a.ZoneId == zoneId &&
                a.AlertType == LocationAlertType.LateAlert &&
                a.TriggeredAt >= todayStart,
                cancellationToken);
    }
}
