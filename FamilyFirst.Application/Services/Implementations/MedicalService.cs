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
    private readonly IMasterDataResolver _masterDataResolver;
    private readonly IErrorCodeService _errorCodeService;

    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;

    public MedicalService(
        IMedicalRepository medicalRepository,
        IFamilyMemberRepository memberRepository,
        ICalendarService calendarService,
        IMasterDataResolver masterDataResolver,
        IErrorCodeService errorCodeService,
        IApiLogService apiLogService,
        IPermissionService permissionService)
    {
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _medicalRepository = medicalRepository;
        _memberRepository  = memberRepository;
        _calendarService   = calendarService;
        _masterDataResolver = masterDataResolver;
        _errorCodeService   = errorCodeService;
    }

    public async Task<IReadOnlyCollection<HealthProfileSummaryDto>> ListHealthProfilesAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var reader = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(reader.Role, FamilyFirstPermission.View, cancellationToken);

        var profiles = await _medicalRepository.ListByFamilyAsync(familyId, cancellationToken);

        return LogResult(nameof(ListHealthProfilesAsync), familyId, profiles.Select(p =>
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
        }).ToArray());
    }

    public async Task<HealthProfileDto> GetHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var member  = await EnsureReadAccessAsync(currentUserId, familyId, cancellationToken);
        // Child can only see their own profile, including before first-use creation.
        if (member.Role == UserRole.Child && memberId != member.Id)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        var dto = profile is not null
            ? MapToHealthProfileDto(profile)
            : await BuildEmptyProfileDtoAsync(member.FamilyId, memberId, cancellationToken);

        // §13.4 rules 12/13: Child sees a limited own view; Elder sees a summary only. DTO shape is unchanged.
        return LogResult(nameof(GetHealthProfileAsync), familyId, ShapeProfileForRole(dto, member.Role));
    }

    public async Task<HealthProfileDto> UpdateHealthProfileAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        UpdateHealthProfileRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var profile = await GetOrCreateProfileAsync(member, familyId, memberId, resolveMemberForWrite: true, cancellationToken);

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
        return LogResult(nameof(UpdateHealthProfileAsync), familyId, MapToHealthProfileDto(profile));
    }

    public async Task<PrescriptionDto> AddPrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var prescMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(prescMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var profile = await GetOrCreateProfileAsync(prescMember, familyId, memberId, resolveMemberForWrite: true, cancellationToken);

        var prescription = new Prescription
        {
            HealthProfileId   = profile.InternalId,
            FamilyId          = prescMember.FamilyId,
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
            FamilyId         = prescMember.FamilyId,
            EventType        = HealthRecordEventType.Prescription,
            EventDate        = request.StartDate.ToDateTime(TimeOnly.MinValue),
            Title            = $"Prescription: {request.MedicationName} {request.Dosage}",
            LinkedVaultDocumentId = null
        }, cancellationToken);

        return LogResult(nameof(AddPrescriptionAsync), familyId, MapToPrescriptionDto(created));
    }

    public async Task DeletePrescriptionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid prescriptionId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);

        await ResolveMemberIdAsync(memberId, member.FamilyId, cancellationToken);

        var prescription = await _medicalRepository.GetPrescriptionByIdAsync(prescriptionId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is null || profile.InternalId != prescription.HealthProfileId)
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        prescription.IsDeleted  = true;
        prescription.DateDeleted  = DateTime.UtcNow;
        await _medicalRepository.UpdatePrescriptionAsync(prescription, cancellationToken);
        LogCompletion(nameof(DeletePrescriptionAsync), familyId);
    }

    public async Task<IReadOnlyCollection<VaccinationDto>> ListVaccinationsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureReadAccessAsync(currentUserId, familyId, cancellationToken);
        if (member.Role == UserRole.Child && member.Id != memberId)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        // Elder may read vaccination status ("upcoming vaccination" summary, §13.4 rule 13).
        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is null)
        {
            await EnsureFamilyMemberExistsAsync(member.FamilyId, memberId, cancellationToken);
            return LogResult(nameof(ListVaccinationsAsync), familyId, Array.Empty<VaccinationDto>());
        }

        var vaccinations = await _medicalRepository.ListVaccinationsAsync(profile.Id, cancellationToken);
        return LogResult(nameof(ListVaccinationsAsync), familyId, vaccinations.Select(MapToVaccinationDto).ToArray());
    }

    public async Task<VaccinationDto> AddVaccinationAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        var vaccMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(vaccMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var profile = await GetOrCreateProfileAsync(vaccMember, familyId, memberId, resolveMemberForWrite: true, cancellationToken);

        var vaccination = new Vaccination
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = vaccMember.FamilyId,
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
                FamilyId        = vaccMember.FamilyId,
                EventType       = HealthRecordEventType.Vaccination,
                EventDate       = request.GivenDate.HasValue ? request.GivenDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.UtcNow,
                Title           = $"Vaccination: {request.VaccineName}",
                LinkedVaultDocumentId = null
            }, cancellationToken);
        }

        return LogResult(nameof(AddVaccinationAsync), familyId, MapToVaccinationDto(created));
    }

    public async Task<VaccinationDto> UpdateVaccinationStatusAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        Guid vaccinationId,
        UpdateVaccinationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        await ResolveMemberIdAsync(memberId, member.FamilyId, cancellationToken);

        var vaccination = await _medicalRepository.GetVaccinationByIdAsync(vaccinationId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is null || profile.InternalId != vaccination.HealthProfileId)
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        vaccination.Status    = request.Status;
        vaccination.GivenDate = request.GivenDate.HasValue ? request.GivenDate.Value.ToDateTime(TimeOnly.MinValue) : vaccination.GivenDate;

        await _medicalRepository.UpdateVaccinationAsync(vaccination, cancellationToken);
        return LogResult(nameof(UpdateVaccinationStatusAsync), familyId, MapToVaccinationDto(vaccination));
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
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.View, cancellationToken);

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is null)
        {
            await EnsureFamilyMemberExistsAsync(member.FamilyId, memberId, cancellationToken);
            return LogResult(nameof(ListTimelineAsync), familyId,
                new PaginatedList<HealthRecordDto>(Array.Empty<HealthRecordDto>(), 0, page, pageSize));
        }

        var paginatedRecords = await _medicalRepository.ListTimelineAsync(
            profile.Id, eventType, fromDate, toDate, page, pageSize, cancellationToken);

        return LogResult(nameof(ListTimelineAsync), familyId, new PaginatedList<HealthRecordDto>(
            paginatedRecords.Items.Select(MapToHealthRecordDto).ToList(),
            paginatedRecords.TotalCount,
            page,
            pageSize));
    }

    public async Task<HealthRecordDto> AddHealthRecordAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHealthRecordRequest request,
        CancellationToken cancellationToken)
    {
        var hrMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(hrMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var profile = await GetOrCreateProfileAsync(hrMember, familyId, memberId, resolveMemberForWrite: true, cancellationToken);

        var record = new HealthRecord
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = hrMember.FamilyId,
            EventType        = request.EventType,
            EventDate        = request.EventDate.ToDateTime(TimeOnly.MinValue),
            Title            = request.Title,
            Notes            = request.Notes,
            LinkedVaultDocumentId = null
        };

        var created = await _medicalRepository.AddHealthRecordAsync(record, cancellationToken);
        return LogResult(nameof(AddHealthRecordAsync), familyId, MapToHealthRecordDto(created));
    }

    public async Task<EmergencyCardDto> GetEmergencyCardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var reader = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(reader.Role, FamilyFirstPermission.View, cancellationToken);
        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        return LogResult(nameof(GetEmergencyCardAsync), familyId, BuildEmergencyCardDto(profile));
    }

    public async Task<EmergencyCardDto> GetEmergencyCardByTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var link = await _medicalRepository.GetEmergencyCardLinkByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Share_Link_Expired, cancellationToken));

        if (link.IsRevoked || link.ExpiresAt < DateTime.UtcNow)
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Share_Link_Expired, cancellationToken));

        link.LastAccessedAt = DateTime.UtcNow;
        await _medicalRepository.UpdateEmergencyCardLinkAsync(link, cancellationToken);

        var profile = link.HealthProfile
            ?? await _medicalRepository.GetByIdAsync(link.HealthProfile?.Id ?? Guid.Empty, link.Family?.Id ?? Guid.Empty, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        return LogResult(nameof(GetEmergencyCardByTokenAsync), null, BuildEmergencyCardDto(profile));
    }

    public async Task<EmergencyCardShareDto> ShareEmergencyCardAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        ShareEmergencyCardRequest request,
        CancellationToken cancellationToken)
    {
        var shareMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(shareMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        await ResolveMemberIdAsync(memberId, shareMember.FamilyId, cancellationToken);

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (!IsProfileComplete(profile))
        {
            throw new UnprocessableEntityException(await GetMessageAsync(FamilyFirstErrorCode.Health_Profile_Incomplete, cancellationToken));
        }

        var expiryHours = request.ExpiryHours ?? DefaultShareExpiryHours;
        var language    = request.Language ?? "en";
        var token       = Convert.ToBase64String(
                              System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var link = new EmergencyCardLink
        {
            HealthProfileId = profile.InternalId,
            FamilyId        = shareMember.FamilyId,
            CreatedByUserId = shareMember.UserId,
            Token           = token,
            Language        = language,
            ExpiresAt       = DateTime.UtcNow.AddHours(expiryHours)
        };

        await _medicalRepository.AddEmergencyCardLinkAsync(link, cancellationToken);

        var shareUrl = $"/medical/emergency-card/{token}";

        return LogResult(nameof(ShareEmergencyCardAsync), familyId, new EmergencyCardShareDto(
            ShareLink:         shareUrl,
            QrCodeData:        shareUrl,
            ShareableImageUrl: null,
            ExpiresAt:         link.ExpiresAt));
    }

    public async Task<IReadOnlyCollection<HeightWeightDto>> ListHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.View, cancellationToken);
        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is null)
        {
            await EnsureFamilyMemberExistsAsync(member.FamilyId, memberId, cancellationToken);
            return LogResult(nameof(ListHeightWeightAsync), familyId, Array.Empty<HeightWeightDto>());
        }

        var records = await _medicalRepository.ListHeightWeightAsync(profile.Id, cancellationToken);
        return LogResult(nameof(ListHeightWeightAsync), familyId, records.Select(r => new HeightWeightDto(r.Id, DateOnly.FromDateTime(r.RecordedDate), r.HeightCm, r.WeightKg)).ToArray());
    }

    public async Task<HeightWeightDto> AddHeightWeightAsync(
        Guid currentUserId,
        Guid familyId,
        Guid memberId,
        AddHeightWeightRequest request,
        CancellationToken cancellationToken)
    {
        var hwMember = await EnsureParentOrAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(hwMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var profile = await GetOrCreateProfileAsync(hwMember, familyId, memberId, resolveMemberForWrite: true, cancellationToken);

        var record = new HeightWeightRecord
        {
            HealthProfileId  = profile.InternalId,
            FamilyId         = hwMember.FamilyId,
            RecordedDate     = request.RecordedDate.ToDateTime(TimeOnly.MinValue),
            HeightCm         = request.HeightCm,
            WeightKg         = request.WeightKg,
            RecordedByUserId = hwMember.UserId
        };

        var created = await _medicalRepository.AddHeightWeightAsync(record, cancellationToken);
        return LogResult(nameof(AddHeightWeightAsync), familyId, new HeightWeightDto(created.Id, DateOnly.FromDateTime(created.RecordedDate), created.HeightCm, created.WeightKg));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<FamilyMember> EnsureParentOrAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.Parent && member.Role != UserRole.FamilyAdmin)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        return member;
    }

    private async Task<FamilyMember> EnsureMemberAccessAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role == UserRole.Teacher || member.Role == UserRole.SuperAdmin)
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        return member;
    }

    // Reads: Teacher/SuperAdmin denied. Elder is allowed only on the summary paths (profile summary, vaccination
    // status — §13.4 rule 13; §18.3 grants Elder no Medical rows). Every other role needs the DB View permission.
    private async Task<FamilyMember> EnsureReadAccessAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureMemberAccessAsync(currentUserId, familyId, cancellationToken);
        if (member.Role != UserRole.Elder)
            await EnsurePermissionAsync(member.Role, FamilyFirstPermission.View, cancellationToken);
        return member;
    }

    private async Task EnsureFamilyMemberExistsAsync(long familyId, Guid memberId, CancellationToken cancellationToken)
    {
        _ = await _memberRepository.GetByIdForFamilyAsync(memberId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
    }

    private async Task<HealthProfileDto> BuildEmptyProfileDtoAsync(long familyId, Guid memberId, CancellationToken cancellationToken)
    {
        var target = await _memberRepository.GetByIdForFamilyAsync(memberId, familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        return new HealthProfileDto(
            Guid.Empty, target.Id, target.DisplayName ?? target.User?.FullName ?? string.Empty,
            string.Empty, Array.Empty<AllergyDto>(), Array.Empty<string>(), null, null, false,
            Array.Empty<PrescriptionDto>(), Array.Empty<VaccinationDto>(), false, DateTime.UtcNow);
    }

    private static HealthProfileDto ShapeProfileForRole(HealthProfileDto dto, UserRole role) => role switch
    {
        UserRole.Elder => dto with
        {
            BloodGroup = string.Empty,
            KnownAllergies = Array.Empty<AllergyDto>(),
            ChronicConditions = Array.Empty<string>(),
            PrimaryDoctor = null,
            EmergencyContact = null,
            OrganDonor = false,
            CurrentMedications = Array.Empty<PrescriptionDto>()
        },
        UserRole.Child => dto with
        {
            ChronicConditions = Array.Empty<string>(),
            PrimaryDoctor = null,
            EmergencyContact = null
        },
        _ => dto
    };

    private async Task<HealthProfile> GetOrCreateProfileAsync(
        FamilyMember caller,
        Guid familyId,
        Guid memberId,
        bool resolveMemberForWrite,
        CancellationToken cancellationToken)
    {
        long? internalMemberId = resolveMemberForWrite
            ? await ResolveMemberIdAsync(memberId, caller.FamilyId, cancellationToken)
            : null;

        var profile = await _medicalRepository.GetByMemberIdAsync(familyId, memberId, cancellationToken);
        if (profile is not null)
            return profile;

        if (!resolveMemberForWrite)
            await EnsurePermissionAsync(caller.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        internalMemberId ??= await ResolveMemberIdAsync(memberId, caller.FamilyId, cancellationToken);
        return await _medicalRepository.AddOrGetProfileAsync(
            new HealthProfile { FamilyId = caller.FamilyId, FamilyMemberId = internalMemberId.Value },
            cancellationToken);
    }

    private async Task<long> ResolveMemberIdAsync(Guid memberId, long familyId, CancellationToken cancellationToken)
    {
        var internalMemberId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.FamilyMember, memberId.ToString(), familyId, cancellationToken);
        return internalMemberId ?? throw new NotFoundException(
            await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
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

    private Task<string> GetMessageAsync(FamilyFirstErrorCode code, CancellationToken cancellationToken) =>
        _errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CheckAsync(role, FamilyFirstModule.MedicalRecords, permission, cancellationToken))
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
}
