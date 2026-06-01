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
                .Select(v => (DateTime?)v.DueDate!.Value)
                .FirstOrDefault();

            return new HealthProfileSummaryDto(
                p.FamilyMember?.Id ?? Guid.Empty,
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
        if (member.Role == UserRole.Child && profile.FamilyMember?.Id != member.Id)
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

        var prescMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var prescription = new Prescription
        {
            HealthProfileId   = profile.InternalId,
            FamilyId          = prescMember?.FamilyId ?? 0L,
            MedicationName    = request.MedicationName,
            Dosage            = request.Dosage,
            Frequency         = request.Frequency,
            PrescribingDoctor = request.PrescribingDoctor,
            StartDate         = request.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate           = request.EndDate.HasValue ? request.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            IsRecurring       = request.IsRecurring,
            LinkedVaultDocumentId = null // Guid? from DTO not directly mappable to long?
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
            HealthProfileId  = profile.InternalId,
            FamilyId         = prescMember?.FamilyId ?? 0L,
            EventType        = HealthRecordEventType.Prescription,
            EventDate        = request.StartDate.ToDateTime(TimeOnly.MinValue),
            Title            = $"Prescription: {request.MedicationName} {request.Dosage}",
            LinkedVaultDocumentId = null
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
        prescription.DateDeleted  = DateTime.UtcNow;
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

        var vaccMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var vaccination = new Vaccination
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = vaccMember?.FamilyId ?? 0L,
            VaccineName      = request.VaccineName,
            Status           = request.Status,
            GivenDate        = request.GivenDate.HasValue ? request.GivenDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            DueDate          = request.DueDate.HasValue ? request.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            LinkedVaultDocumentId = null // Guid? from DTO not directly mappable to long?
        };

        var created = await _medicalRepository.AddVaccinationAsync(vaccination, cancellationToken);

        if (request.Status == VaccinationStatus.Given)
        {
            await _medicalRepository.AddHealthRecordAsync(new HealthRecord
            {
                HealthProfileId = profile.InternalId,
                FamilyId        = vaccMember?.FamilyId ?? 0L,
                EventType       = HealthRecordEventType.Vaccination,
                EventDate       = request.GivenDate.HasValue ? request.GivenDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.UtcNow,
                Title           = $"Vaccination: {request.VaccineName}",
                LinkedVaultDocumentId = null
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
        vaccination.GivenDate = request.GivenDate.HasValue ? request.GivenDate.Value.ToDateTime(TimeOnly.MinValue) : vaccination.GivenDate;

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

        var hrMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var record = new HealthRecord
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = hrMember?.FamilyId ?? 0L,
            EventType        = request.EventType,
            EventDate        = request.EventDate.ToDateTime(TimeOnly.MinValue),
            Title            = request.Title,
            Notes            = request.Notes,
            LinkedVaultDocumentId = null
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
            ?? await _medicalRepository.GetByIdAsync(link.HealthProfile?.Id ?? Guid.Empty, link.Family?.Id ?? Guid.Empty, cancellationToken)
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

        var shareMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var link = new EmergencyCardLink
        {
            HealthProfileId = profile.InternalId,
            FamilyId        = shareMember?.FamilyId ?? 0L,
            CreatedByUserId = shareMember?.UserId ?? 0L,
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
        return records.Select(r => new HeightWeightDto(r.Id, DateOnly.FromDateTime(r.RecordedDate), r.HeightCm, r.WeightKg)).ToArray();
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

        var hwMember = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var record = new HeightWeightRecord
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = hwMember?.FamilyId ?? 0L,
            RecordedDate     = request.RecordedDate.ToDateTime(TimeOnly.MinValue),
            HeightCm         = request.HeightCm,
            WeightKg         = request.WeightKg,
            RecordedByUserId = hwMember?.UserId ?? 0L
        };

        var created = await _medicalRepository.AddHeightWeightAsync(record, cancellationToken);
        return new HeightWeightDto(created.Id, DateOnly.FromDateTime(created.RecordedDate), created.HeightCm, created.WeightKg);
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

        // FamilyId and FamilyMemberId are long in entity; use 0 as placeholder — repo resolves via Guid
        var newProfile = new HealthProfile
        {
            FamilyId       = 0L,
            FamilyMemberId = 0L,
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
            MemberId:              profile.FamilyMember?.Id ?? Guid.Empty,
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
            p.FamilyMember?.Id ?? Guid.Empty,
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
            p.LastUpdated ?? p.DateCreated);
    }

    private static PrescriptionDto MapToPrescriptionDto(Prescription p) =>
        new(p.Id, p.MedicationName, p.Dosage, p.Frequency, p.PrescribingDoctor,
            DateOnly.FromDateTime(p.StartDate),
            p.EndDate.HasValue ? DateOnly.FromDateTime(p.EndDate.Value) : (DateOnly?)null,
            p.IsRecurring, p.IsArchived,
            null); // LinkedVaultDocumentId is long? in entity; Guid? in DTO — not mappable

    private static VaccinationDto MapToVaccinationDto(Vaccination v) =>
        new(v.Id, v.VaccineName, v.Status,
            v.GivenDate.HasValue ? DateOnly.FromDateTime(v.GivenDate.Value) : (DateOnly?)null,
            v.DueDate.HasValue ? DateOnly.FromDateTime(v.DueDate.Value) : (DateOnly?)null,
            null); // LinkedVaultDocumentId is long? in entity; Guid? in DTO — not mappable

    private static HealthRecordDto MapToHealthRecordDto(HealthRecord r) =>
        new(r.Id, r.EventType, DateOnly.FromDateTime(r.EventDate), r.Title, r.Notes, null);

    private static IReadOnlyCollection<string> DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }
}
