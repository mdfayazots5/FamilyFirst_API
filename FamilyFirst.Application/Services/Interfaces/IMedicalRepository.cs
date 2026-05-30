using FamilyFirst.Application.Common.Models;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IMedicalRepository
{
    // Health Profiles
    Task<IReadOnlyCollection<HealthProfile>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<HealthProfile?> GetByMemberIdAsync(Guid familyId, Guid familyMemberId, CancellationToken cancellationToken);

    Task<HealthProfile?> GetByIdAsync(Guid healthProfileId, Guid familyId, CancellationToken cancellationToken);

    Task<HealthProfile> AddAsync(HealthProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(HealthProfile profile, CancellationToken cancellationToken);

    // Prescriptions
    Task<IReadOnlyCollection<Prescription>> ListActivePrescriptionsAsync(Guid healthProfileId, CancellationToken cancellationToken);

    Task<Prescription?> GetPrescriptionByIdAsync(Guid prescriptionId, Guid familyId, CancellationToken cancellationToken);

    Task<Prescription> AddPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken);

    Task UpdatePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Prescription>> GetPrescriptionsDueForArchiveAsync(CancellationToken cancellationToken);

    // Vaccinations
    Task<IReadOnlyCollection<Vaccination>> ListVaccinationsAsync(Guid healthProfileId, CancellationToken cancellationToken);

    Task<Vaccination?> GetVaccinationByIdAsync(Guid vaccinationId, Guid familyId, CancellationToken cancellationToken);

    Task<Vaccination> AddVaccinationAsync(Vaccination vaccination, CancellationToken cancellationToken);

    Task UpdateVaccinationAsync(Vaccination vaccination, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Vaccination>> GetVaccinationsDueForReminderAsync(int withinDays, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Vaccination>> GetOverdueVaccinationsAsync(CancellationToken cancellationToken);

    // Health Records (Timeline)
    Task<PaginatedList<HealthRecord>> ListTimelineAsync(
        Guid healthProfileId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<HealthRecord> AddHealthRecordAsync(HealthRecord record, CancellationToken cancellationToken);

    // Height/Weight
    Task<IReadOnlyCollection<HeightWeightRecord>> ListHeightWeightAsync(Guid healthProfileId, CancellationToken cancellationToken);

    Task<HeightWeightRecord> AddHeightWeightAsync(HeightWeightRecord record, CancellationToken cancellationToken);

    // Emergency Card Links
    Task<EmergencyCardLink?> GetEmergencyCardLinkByTokenAsync(string token, CancellationToken cancellationToken);

    Task<EmergencyCardLink> AddEmergencyCardLinkAsync(EmergencyCardLink link, CancellationToken cancellationToken);

    Task UpdateEmergencyCardLinkAsync(EmergencyCardLink link, CancellationToken cancellationToken);
}
