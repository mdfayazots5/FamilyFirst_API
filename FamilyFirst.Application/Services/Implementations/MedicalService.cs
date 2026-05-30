using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Calendar;
using FamilyFirst.Application.DTOs.Medical;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class MedicalService : IMedicalService
{
    private const int DefaultShareExpiryHours = 72;

    private readonly IMedicalRepository _medicalRepository;
    private readonly IFamilyMemberRepository _memberRepository;
    private readonly ICalendarService _calendarService;

    public MedicalService(
        IMedicalRepository medicalRepository,
        IFamilyMemberRepository memberRepository,
        ICalendarService calendarService)
    {
        _medicalRepository = medicalRepository;
        _memberRepository  = memberRepository;
        _calendarService   = calendarService;
    }

    public async Task<IReadOnlyCollection<HealthProfileSummaryDto>> ListHealthProfilesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var profiles = await _medicalRepository.ListByFamilyAsync(familyId, cancellationToken);

        return profiles.Select(p =>
        {
            var allergies    = ParseAllergies(p.KnownAllergiesJson);
            var activeMeds   = p.Prescriptions.Count(pr => !pr.IsArchived);
            var nextVaccDue  = p.Vaccinations
                .Where(v => v.Status == VaccinationStatus.Due && v.DueDate.HasValue)
                .OrderBy(v => v.DueDate)
                .Select(v => (DateTime?)v.DueDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
                .FirstOrDefault();

            return new HealthProfileSummaryDto(
                p.FamilyMemberId,
                p.FamilyMember?.DisplayName ?? string.Empty,
                p.BloodGroup,
                allergies.Count > 0,
                activeMeds,
                nextVaccDue,
                IsProfileComplete(p));
        }).ToArray();
    }

    public async Task<HealthProfileDto> GetHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var member  = await EnsureMemberAccessAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        // Child can only see their own profile
        if (member.Role == UserRole.Child && profile.FamilyMemberId != member.Id)
            throw new ForbiddenAccessException();

        // Elder gets summary only — enforced in controller role gate; service returns full DTO
        return MapToHealthProfileDto(profile);
    }

    public async Task<HealthProfileDto> UpdateHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        UpdateHealthProfileRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        profile.BloodGroup                    = request.BloodGroup ?? profile.BloodGroup;
        profile.KnownAllergiesJson            = request.KnownAllergies != null
            ? System.Text.Json.JsonSerializer.Serialize(request.KnownAllergies)
            : profile.KnownAllergiesJson;
        profile.ChronicConditionsJson         = request.ChronicConditions != null
            ? System.Text.Json.JsonSerializer.Serialize(request.ChronicConditions)
            : profile.ChronicConditionsJson;
        profile.PrimaryDoctorName             = request.PrimaryDoctorName             ?? profile.PrimaryDoctorName;
        profile.PrimaryDoctorPhone            = request.PrimaryDoctorPhone            ?? profile.PrimaryDoctorPhone;
        profile.EmergencyContactName          = request.EmergencyContactName          ?? profile.EmergencyContactName;
        profile.EmergencyContactRelationship  = request.EmergencyContactRelationship  ?? profile.EmergencyContactRelationship;
        profile.EmergencyContactPhone         = request.EmergencyContactPhone         ?? profile.EmergencyContactPhone;
        if (request.OrganDonor.HasValue) profile.OrganDonor = request.OrganDonor.Value;

        await _medicalRepository.UpdateAsync(profile, cancellationToken);
        return MapToHealthProfileDto(profile);
    }

    public async Task<PrescriptionDto> AddPrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        var prescription = new Prescription
        {
            HealthProfileId   = profile.Id,
            FamilyId          = familyId,
            MedicationName    = request.MedicationName,
            Dosage            = request.Dosage,
            Frequency         = request.Frequency,
            PrescribingDoctor = request.PrescribingDoctor,
            StartDate         = request.StartDate,
            EndDate           = request.EndDate,
            IsRecurring       = request.IsRecurring,
            LinkedDocumentId  = request.LinkedDocumentId
        };

        var created = await _medicalRepository.AddPrescriptionAsync(prescription, cancellationToken);

        if (request.IsRecurring)
        {
            var startUtc = request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            await _calendarService.CreateEventAsync(currentUserId, familyId, new CreateEventRequest
            {
                EventTitle      = $"Medication: {request.MedicationName} {request.Dosage}",
                EventType       = EventType.MedicineReminder,
                StartDateTime   = startUtc,
                IsRecurring     = true,
                RecurrenceRule  = "FREQ=DAILY",
                VisibilityScope = "Parent"
            }, cancellationToken);
        }

        // Record timeline entry
        await _medicalRepository.AddHealthRecordAsync(new HealthRecord
        {
            HealthProfileId  = profile.Id,
            FamilyId         = familyId,
            EventType        = HealthRecordEventType.Prescription,
            EventDate        = request.StartDate,
            Title            = $"Prescription: {request.MedicationName} {request.Dosage}",
            LinkedDocumentId = request.LinkedDocumentId
        }, cancellationToken);

        return MapToPrescriptionDto(created);
    }

    public async Task DeletePrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid prescriptionId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var prescription = await _medicalRepository.GetPrescriptionByIdAsync(prescriptionId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        prescription.IsDeleted  = true;
        prescription.DeletedAt  = DateTime.UtcNow;
        await _medicalRepository.UpdatePrescriptionAsync(prescription, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VaccinationDto>> ListVaccinationsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        await EnsureMemberAccessAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);
        var vaccinations = await _medicalRepository.ListVaccinationsAsync(profile.Id, cancellationToken);
        return vaccinations.Select(MapToVaccinationDto).ToArray();
    }

    public async Task<VaccinationDto> AddVaccinationAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        var vaccination = new Vaccination
        {
            HealthProfileId  = profile.Id,
            FamilyId         = familyId,
            VaccineName      = request.VaccineName,
            Status           = request.Status,
            GivenDate        = request.GivenDate,
            DueDate          = request.DueDate,
            LinkedDocumentId = request.LinkedDocumentId
        };

        var created = await _medicalRepository.AddVaccinationAsync(vaccination, cancellationToken);

        if (request.Status == VaccinationStatus.Given)
        {
            await _medicalRepository.AddHealthRecordAsync(new HealthRecord
            {
                HealthProfileId = profile.Id,
                FamilyId        = familyId,
                EventType       = HealthRecordEventType.Vaccination,
                EventDate       = request.GivenDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Title           = $"Vaccination: {request.VaccineName}",
                LinkedDocumentId = request.LinkedDocumentId
            }, cancellationToken);
        }

        return MapToVaccinationDto(created);
    }

    public async Task<VaccinationDto> UpdateVaccinationStatusAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid vaccinationId,
        UpdateVaccinationStatusRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var vaccination = await _medicalRepository.GetVaccinationByIdAsync(vaccinationId, familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vaccination), vaccinationId);

        vaccination.Status    = request.Status;
        vaccination.GivenDate = request.GivenDate ?? vaccination.GivenDate;

        await _medicalRepository.UpdateVaccinationAsync(vaccination, cancellationToken);
        return MapToVaccinationDto(vaccination);
    }

    public async Task<PaginatedList<HealthRecordDto>> ListTimelineAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var member = await EnsureMemberAccessAsync(currentUserId, familyId, cancellationToken);
        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException();

        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        var paginatedRecords = await _medicalRepository.ListTimelineAsync(
            profile.Id, eventType, fromDate, toDate, page, pageSize, cancellationToken);

        return new PaginatedList<HealthRecordDto>(
            paginatedRecords.Items.Select(MapToHealthRecordDto).ToList(),
            paginatedRecords.TotalCount,
            page,
            pageSize);
    }

    public async Task<HealthRecordDto> AddHealthRecordAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHealthRecordRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        var record = new HealthRecord
        {
            HealthProfileId  = profile.Id,
            FamilyId         = familyId,
            EventType        = request.EventType,
            EventDate        = request.EventDate,
            Title            = request.Title,
            Notes            = request.Notes,
            LinkedDocumentId = request.LinkedDocumentId
        };

        var created = await _medicalRepository.AddHealthRecordAsync(record, cancellationToken);
        return MapToHealthRecordDto(created);
    }

    public async Task<EmergencyCardDto> GetEmergencyCardAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken)
            ?? throw new NotFoundException("Health profile not found for this member.");
        return BuildEmergencyCardDto(profile);
    }

    public async Task<EmergencyCardDto> GetEmergencyCardByTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var link = await _medicalRepository.GetEmergencyCardLinkByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException("Emergency card link not found or expired.");

        if (link.IsRevoked || link.ExpiresAt < DateTime.UtcNow)
            throw new NotFoundException("Emergency card link not found or expired.");

        link.LastAccessedAt = DateTime.UtcNow;
        await _medicalRepository.UpdateEmergencyCardLinkAsync(link, cancellationToken);

        var profile = link.HealthProfile
            ?? await _medicalRepository.GetByIdAsync(link.HealthProfileId, link.FamilyId, cancellationToken)
            ?? throw new NotFoundException("Health profile not found.");

        return BuildEmergencyCardDto(profile);
    }

    public async Task<EmergencyCardShareDto> ShareEmergencyCardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        ShareEmergencyCardRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken)
            ?? throw new NotFoundException("Health profile not found for this member.");

        if (!IsProfileComplete(profile))
        {
            throw new UnprocessableEntityException(
                "Health profile is incomplete. Add Blood Group and Allergies before sharing the emergency card.");
        }

        var expiryHours = request.ExpiryHours ?? DefaultShareExpiryHours;
        var language    = request.Language ?? "en";
        var token       = Convert.ToBase64String(
                              System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var link = new EmergencyCardLink
        {
            HealthProfileId = profile.Id,
            FamilyId        = familyId,
            CreatedByUserId = currentUserId,
            Token           = token,
            Language        = language,
            ExpiresAt       = DateTime.UtcNow.AddHours(expiryHours)
        };

        await _medicalRepository.AddEmergencyCardLinkAsync(link, cancellationToken);

        var shareUrl = $"/medical/emergency-card/{token}";

        return new EmergencyCardShareDto(
            ShareLink:         shareUrl,
            QrCodeData:        shareUrl,
            ShareableImageUrl: null,
            ExpiresAt:         link.ExpiresAt);
    }

    public async Task<IReadOnlyCollection<HeightWeightDto>> ListHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);
        var records = await _medicalRepository.ListHeightWeightAsync(profile.Id, cancellationToken);
        return records.Select(r => new HeightWeightDto(r.Id, r.RecordedDate, r.HeightCm, r.WeightKg)).ToArray();
    }

    public async Task<HeightWeightDto> AddHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHeightWeightRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        var profile = await GetOrCreateProfileAsync(familyId, memberId, cancellationToken);

        var record = new HeightWeightRecord
        {
            HealthProfileId  = profile.Id,
            FamilyId         = familyId,
            RecordedDate     = request.RecordedDate,
            HeightCm         = request.HeightCm,
            WeightKg         = request.WeightKg,
            RecordedByUserId = currentUserId
        };

        var created = await _medicalRepository.AddHeightWeightAsync(record, cancellationToken);
        return new HeightWeightDto(created.Id, created.RecordedDate, created.HeightCm, created.WeightKg);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<FamilyMember> EnsureParentOrAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException();

        return member;
    }

    private async Task<FamilyMember> EnsureMemberAccessAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (member.Role == UserRole.Teacher || member.Role == UserRole.SuperAdmin)
            throw new ForbiddenAccessException();

        return member;
    }

    private async Task<HealthProfile> GetOrCreateProfileAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);

        if (profile is not null) return profile;

        var newProfile = new HealthProfile
        {
            FamilyId       = familyId,
            FamilyMemberId = memberId,
            BloodGroup     = string.Empty
        };

        return await _medicalRepository.AddAsync(newProfile, cancellationToken);
    }

    private EmergencyCardDto BuildEmergencyCardDto(HealthProfile profile)
    {
        var allergies    = ParseAllergies(profile.KnownAllergiesJson);
        var activeMeds   = profile.Prescriptions
            .Where(p => !p.IsArchived)
            .Select(p => new ActiveMedicationDto(p.MedicationName, p.Dosage))
            .ToArray();

        return new EmergencyCardDto(
            MemberId:              profile.FamilyMemberId,
            MemberName:            profile.FamilyMember?.DisplayName ?? string.Empty,
            MemberPhotoUrl:        null,
            AgeYears:              null,
            BloodGroup:            profile.BloodGroup,
            KnownAllergies:        allergies.Select(a => new AllergyDto(a.Text, a.Category)).ToArray(),
            CurrentMedications:    activeMeds,
            PrimaryDoctorName:     profile.PrimaryDoctorName,
            PrimaryDoctorPhone:    profile.PrimaryDoctorPhone,
            EmergencyContactName:  profile.EmergencyContactName,
            EmergencyContactPhone: profile.EmergencyContactPhone,
            OrganDonor:            profile.OrganDonor,
            IsProfileComplete:     IsProfileComplete(profile));
    }

    private static bool IsProfileComplete(HealthProfile profile) =>
        !string.IsNullOrWhiteSpace(profile.BloodGroup) &&
        !string.IsNullOrWhiteSpace(profile.KnownAllergiesJson) &&
        profile.KnownAllergiesJson != "[]";

    private static List<AllergyInput> ParseAllergies(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return System.Text.Json.JsonSerializer.Deserialize<List<AllergyInput>>(json) ?? new(); }
        catch { return new(); }
    }

    private static HealthProfileDto MapToHealthProfileDto(HealthProfile p)
    {
        var allergies   = ParseAllergies(p.KnownAllergiesJson)
            .Select(a => new AllergyDto(a.Text, a.Category)).ToArray();
        var conditions  = DeserializeStringArray(p.ChronicConditionsJson);
        var activeMeds  = p.Prescriptions
            .Where(pr => !pr.IsArchived)
            .Select(MapToPrescriptionDto).ToArray();
        var vaccinations = p.Vaccinations.Select(MapToVaccinationDto).ToArray();

        return new HealthProfileDto(
            p.Id,
            p.FamilyMemberId,
            p.FamilyMember?.DisplayName ?? string.Empty,
            p.BloodGroup,
            allergies,
            conditions,
            p.PrimaryDoctorName != null ? new DoctorDto(p.PrimaryDoctorName, p.PrimaryDoctorPhone) : null,
            p.EmergencyContactName != null
                ? new ContactDto(p.EmergencyContactName, p.EmergencyContactRelationship, p.EmergencyContactPhone)
                : null,
            p.OrganDonor,
            activeMeds,
            vaccinations,
            IsProfileComplete(p),
            p.UpdatedAt);
    }

    private static PrescriptionDto MapToPrescriptionDto(Prescription p) =>
        new(p.Id, p.MedicationName, p.Dosage, p.Frequency, p.PrescribingDoctor,
            p.StartDate, p.EndDate, p.IsRecurring, p.IsArchived, p.LinkedDocumentId);

    private static VaccinationDto MapToVaccinationDto(Vaccination v) =>
        new(v.Id, v.VaccineName, v.Status, v.GivenDate, v.DueDate, v.LinkedDocumentId);

    private static HealthRecordDto MapToHealthRecordDto(HealthRecord r) =>
        new(r.Id, r.EventType, r.EventDate, r.Title, r.Notes, r.LinkedDocumentId);

    private static IReadOnlyCollection<string> DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }
}
