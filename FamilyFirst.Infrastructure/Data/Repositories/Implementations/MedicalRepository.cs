using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class MedicalRepository : IMedicalRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public MedicalRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<HealthProfile>> ListByFamilyAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        return await QueryProfiles()
            .Where(p => p.Family.Id == familyId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<HealthProfile?> GetByMemberIdAsync(
        Guid familyId, Guid familyMemberId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(p => p.Family.Id == familyId && p.FamilyMember.Id == familyMemberId, cancellationToken);
    }

    public Task<HealthProfile?> GetByIdAsync(
        Guid healthProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(p => p.Id == healthProfileId && p.Family.Id == familyId, cancellationToken);
    }

    public async Task<HealthProfile> AddAsync(HealthProfile profile, CancellationToken cancellationToken)
    {
        _dbContext.Set<HealthProfile>().Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task UpdateAsync(HealthProfile profile, CancellationToken cancellationToken)
    {
        _dbContext.Set<HealthProfile>().Update(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Prescription>> ListActivePrescriptionsAsync(
        Guid healthProfileId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Prescription>()
            .Where(p => p.HealthProfile.Id == healthProfileId && !p.IsArchived)
            .OrderBy(p => p.StartDate)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Prescription?> GetPrescriptionByIdAsync(
        Guid prescriptionId, Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Prescription>()
            .SingleOrDefaultAsync(p => p.Id == prescriptionId && p.Family.Id == familyId, cancellationToken);
    }

    public async Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken)
    {
        _dbContext.Set<Prescription>().Add(prescription);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return prescription;
    }

    public async Task UpdatePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken)
    {
        _dbContext.Set<Prescription>().Update(prescription);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Prescription>> GetPrescriptionsDueForArchiveAsync(
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.Set<Prescription>()
            .Where(p => !p.IsArchived && p.EndDate.HasValue && p.EndDate.Value < today)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Vaccination>> ListVaccinationsAsync(
        Guid healthProfileId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Vaccination>()
            .Where(v => v.HealthProfile.Id == healthProfileId)
            .OrderBy(v => v.VaccineName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Vaccination?> GetVaccinationByIdAsync(
        Guid vaccinationId, Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Vaccination>()
            .SingleOrDefaultAsync(v => v.Id == vaccinationId && v.Family.Id == familyId, cancellationToken);
    }

    public async Task<Vaccination> AddVaccinationAsync(Vaccination vaccination, CancellationToken cancellationToken)
    {
        _dbContext.Set<Vaccination>().Add(vaccination);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return vaccination;
    }

    public async Task UpdateVaccinationAsync(Vaccination vaccination, CancellationToken cancellationToken)
    {
        _dbContext.Set<Vaccination>().Update(vaccination);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Vaccination>> GetVaccinationsDueForReminderAsync(
        int withinDays, CancellationToken cancellationToken)
    {
        var today   = DateTime.UtcNow.Date;
        var cutoff  = today.AddDays(withinDays);
        return await _dbContext.Set<Vaccination>()
            .Where(v => v.Status == Domain.Enums.VaccinationStatus.Due
                     && v.DueDate.HasValue
                     && v.DueDate.Value >= today
                     && v.DueDate.Value <= cutoff)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Vaccination>> GetOverdueVaccinationsAsync(
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbContext.Set<Vaccination>()
            .Where(v => v.Status == Domain.Enums.VaccinationStatus.Due
                     && v.DueDate.HasValue
                     && v.DueDate.Value < today)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedList<HealthRecord>> ListTimelineAsync(
        Guid healthProfileId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<HealthRecord>()
            .Where(r => r.HealthProfile.Id == healthProfileId);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(r => r.EventType == eventType);

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.EventDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.EventDate <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.EventDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PaginatedList<HealthRecord>(items, totalCount, page, pageSize);
    }

    public async Task<HealthRecord> AddHealthRecordAsync(HealthRecord record, CancellationToken cancellationToken)
    {
        _dbContext.Set<HealthRecord>().Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyCollection<HeightWeightRecord>> ListHeightWeightAsync(
        Guid healthProfileId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<HeightWeightRecord>()
            .Where(h => h.HealthProfile.Id == healthProfileId)
            .OrderByDescending(h => h.RecordedDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<HeightWeightRecord> AddHeightWeightAsync(HeightWeightRecord record, CancellationToken cancellationToken)
    {
        _dbContext.Set<HeightWeightRecord>().Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public Task<EmergencyCardLink?> GetEmergencyCardLinkByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmergencyCardLink>()
            .Include(l => l.HealthProfile)
            .ThenInclude(hp => hp!.FamilyMember)
            .Include(l => l.HealthProfile)
            .ThenInclude(hp => hp!.Prescriptions.Where(p => !p.IsArchived))
            .Include(l => l.HealthProfile)
            .ThenInclude(hp => hp!.Vaccinations)
            .SingleOrDefaultAsync(l => l.Token == token, cancellationToken);
    }

    public async Task<EmergencyCardLink> AddEmergencyCardLinkAsync(EmergencyCardLink link, CancellationToken cancellationToken)
    {
        _dbContext.Set<EmergencyCardLink>().Add(link);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return link;
    }

    public async Task UpdateEmergencyCardLinkAsync(EmergencyCardLink link, CancellationToken cancellationToken)
    {
        _dbContext.Set<EmergencyCardLink>().Update(link);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<HealthProfile> QueryProfiles()
    {
        return _dbContext.Set<HealthProfile>()
            .Include(p => p.FamilyMember)
            .Include(p => p.Prescriptions.Where(pr => !pr.IsDeleted))
            .Include(p => p.Vaccinations.Where(v => !v.IsDeleted))
            .Include(p => p.EmergencyCardLinks.Where(l => !l.IsDeleted && !l.IsRevoked));
    }
}
