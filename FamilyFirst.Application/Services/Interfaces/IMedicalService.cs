using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Medical;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IMedicalService
{
    Task<IReadOnlyCollection<HealthProfileSummaryDto>> ListHealthProfilesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken);

    Task<HealthProfileDto> GetHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<HealthProfileDto> UpdateHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        UpdateHealthProfileRequest request,
        CancellationToken cancellationToken);

    Task<PrescriptionDto> AddPrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddPrescriptionRequest request,
        CancellationToken cancellationToken);

    Task DeletePrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid prescriptionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VaccinationDto>> ListVaccinationsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<VaccinationDto> AddVaccinationAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddVaccinationRequest request,
        CancellationToken cancellationToken);

    Task<VaccinationDto> UpdateVaccinationStatusAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid vaccinationId,
        UpdateVaccinationStatusRequest request,
        CancellationToken cancellationToken);

    Task<PaginatedList<HealthRecordDto>> ListTimelineAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<HealthRecordDto> AddHealthRecordAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHealthRecordRequest request,
        CancellationToken cancellationToken);

    Task<EmergencyCardDto> GetEmergencyCardAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<EmergencyCardDto> GetEmergencyCardByTokenAsync(
        string token,
        CancellationToken cancellationToken);

    Task<EmergencyCardShareDto> ShareEmergencyCardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        ShareEmergencyCardRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HeightWeightDto>> ListHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<HeightWeightDto> AddHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHeightWeightRequest request,
        CancellationToken cancellationToken);
}
