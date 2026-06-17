using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class FamilyService : IFamilyService
{
    private const string FreeTrialPlanCode = "free_trial";
    private const int JoinCodeLength = 6;
    private const string JoinCodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IFamilyRepository _familyRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public FamilyService(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IFeedbackRepository feedbackRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherProfileRepository teacherProfileRepository,
        IUserRepository userRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
        _feedbackRepository = feedbackRepository;
        _childProfileRepository = childProfileRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _userRepository = userRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<FamilyDto> CreateFamilyAsync(Guid currentUserId, CreateFamilyRequest request, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        if (await _familyRepository.UserOwnsActiveFamilyAsync(currentUserId, cancellationToken))
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        var plan = await _familyRepository.GetPlanByCodeAsync(FreeTrialPlanCode, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
        var family = CreateFamily(request, currentUserId, plan, joinCode);
        var subscription = CreateTrialSubscription(family.Id, plan);
        var familyMember = CreateFamilyAdminMember(family.Id, currentUserId);

        await _familyRepository.AddFamilyGraphAsync(family, subscription, familyMember, cancellationToken);

        var response = ToFamilyDto(family, plan.PlanCode);
        LogApiCall(nameof(CreateFamilyAsync), new { currentUserId, request.FamilyName, request.City }, new { response.FamilyId, response.PlanCode });
        return response;
    }

    public async Task<FamilyDto> GetFamilyAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        var response = ToFamilyDto(family);
        LogApiCall(nameof(GetFamilyAsync), new { currentUserId, familyId }, new { response.FamilyId, response.PlanCode });
        return response;
    }

    public async Task<FamilyDto> UpdateFamilyAsync(Guid currentUserId, Guid familyId, UpdateFamilyRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        family.FamilyName = request.FamilyName.Trim();
        family.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        await _familyRepository.UpdateAsync(family, cancellationToken);

        var response = ToFamilyDto(family);
        LogApiCall(nameof(UpdateFamilyAsync), new { currentUserId, familyId, request.FamilyName, request.City }, new { response.FamilyId, response.FamilyName });
        return response;
    }

    public async Task<string> GetJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, null, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        LogApiCall(nameof(GetJoinCodeAsync), new { currentUserId, familyId }, new { family.Id, family.JoinCode });
        return family.JoinCode;
    }

    public async Task<string> RegenerateJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        family.JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);

        await _familyRepository.UpdateAsync(family, cancellationToken);

        LogApiCall(nameof(RegenerateJoinCodeAsync), new { currentUserId, familyId }, new { family.Id, family.JoinCode });
        return family.JoinCode;
    }

    public async Task<FamilyMemberDto> JoinFamilyAsync(Guid currentUserId, JoinFamilyRequest request, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);
        await EnsureAssignableRoleAsync(request.Role, cancellationToken);

        var family = await _familyRepository.GetByJoinCodeAsync(request.JoinCode.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));

        if (await _familyMemberRepository.GetActiveByFamilyAndUserAsync(family.Id, currentUserId, cancellationToken) is not null)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.User_Not_Found, cancellationToken));
        user.FullName = request.FullName.Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var familyMember = CreateFamilyMember(family.Id, currentUserId, request.Role, request.LinkType, null, null);
        await _familyMemberRepository.AddAsync(familyMember, cancellationToken);
        await EnsureRoleProfileAsync(familyMember, cancellationToken);

        var response = ToFamilyMemberDto(familyMember, user);
        LogApiCall(nameof(JoinFamilyAsync), new { currentUserId, request.JoinCode, request.Role, request.LinkType }, new { response.FamilyMemberId, response.Role, response.FamilyId });
        return response;
    }

    public async Task<IReadOnlyCollection<FamilyMemberDto>> ListMembersAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        var members = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var response = members.Select(ToFamilyMemberDto).ToArray();
        LogApiCall(nameof(ListMembersAsync), new { currentUserId, familyId }, new { Count = response.Length });
        return response;
    }

    public async Task<FamilyMemberDto> AddMemberAsync(Guid currentUserId, Guid familyId, AddMemberRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);
        await EnsureAssignableRoleAsync(request.Role, cancellationToken);
        await EnsurePlanLimitAsync(familyId, request.Role, cancellationToken);

        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var user = await GetOrCreateMemberUserAsync(phoneNumber, request.FullName.Trim(), cancellationToken);

        if (await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, user.Id, cancellationToken) is not null)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        var familyMember = CreateFamilyMember(familyId, user.Id, request.Role, request.LinkType, null, currentUserId);
        await _familyMemberRepository.AddAsync(familyMember, cancellationToken);
        await EnsureRoleProfileAsync(familyMember, cancellationToken);

        var response = ToFamilyMemberDto(familyMember, user);
        LogApiCall(nameof(AddMemberAsync), new { currentUserId, familyId, request.Role, request.LinkType, PhoneNumber = MaskPhoneNumber(phoneNumber) }, new { response.FamilyMemberId, response.UserId, response.Role });
        return response;
    }

    public async Task<FamilyMemberDto> UpdateMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);
        await EnsureAssignableRoleAsync(request.Role, cancellationToken);
        await EnsurePlanLimitAsync(familyId, request.Role, cancellationToken, memberId);

        var member = await GetFamilyMemberOrThrowAsync(memberId, familyId, cancellationToken);
        member.Role = request.Role;
        member.LinkType = request.LinkType.Trim();
        member.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

        await _familyMemberRepository.UpdateAsync(member, cancellationToken);
        await EnsureRoleProfileAsync(member, cancellationToken);

        var response = ToFamilyMemberDto(member);
        LogApiCall(nameof(UpdateMemberAsync), new { currentUserId, familyId, memberId, request.Role, request.LinkType }, new { response.FamilyMemberId, response.Role, response.DisplayName });
        return response;
    }

    public async Task<bool> RemoveMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.Delete, cancellationToken);

        var member = await GetFamilyMemberOrThrowAsync(memberId, familyId, cancellationToken);
        await EnsureFamilyAdminRemovalAllowedAsync(familyId, member, cancellationToken);

        member.IsDeleted = true;
        member.IsActive = false;
        member.DeletedAt = DateTime.UtcNow;

        await _familyMemberRepository.UpdateAsync(member, cancellationToken);

        LogApiCall(nameof(RemoveMemberAsync), new { currentUserId, familyId, memberId }, new { Removed = true });
        return true;
    }

    public async Task<FamilyDashboardDto> GetDashboardAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var currentMember = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (currentMember.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        var members = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var unacknowledgedFeedbackCount = await _feedbackRepository.CountUnacknowledgedByFamilyAsync(familyId, cancellationToken);

        var response = new FamilyDashboardDto(
            family.Id,
            family.FamilyName,
            DateOnly.FromDateTime(DateTime.UtcNow),
            family.FamilyScore,
            family.CurrentStreakDays,
            family.BestStreakDays,
            unacknowledgedFeedbackCount,
            members.Count,
            members.Count(member => member.Role == UserRole.Parent || member.Role == UserRole.FamilyAdmin),
            members.Count(member => member.Role == UserRole.Child),
            members.Count(member => member.Role == UserRole.Teacher),
            members.Count(member => member.Role == UserRole.Elder));
        LogApiCall(nameof(GetDashboardAsync), new { currentUserId, familyId }, new { response.FamilyId, response.UnacknowledgedFeedbackCount, response.TotalMembers });
        return response;
    }

    private async Task<Family> GetFamilyOrThrowAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
    }

    private async Task<FamilyMember> GetFamilyMemberOrThrowAsync(Guid memberId, Guid familyId, CancellationToken cancellationToken)
    {
        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        var resolvedMemberId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.FamilyMember,
            memberId.ToString(),
            family.InternalId,
            cancellationToken);

        if (!resolvedMemberId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var member = await _familyMemberRepository.GetByIdAsync(memberId, cancellationToken);

        if (member is null || member.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return member;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task EnsureFamilyAdminAsync(Guid currentUserId, Guid familyId, FamilyFirstPermission? permission, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (permission.HasValue)
        {
            await EnsurePermissionAsync(member.Role, permission.Value, cancellationToken);
        }
    }

    private async Task EnsurePlanLimitAsync(Guid familyId, UserRole role, CancellationToken cancellationToken, Guid? ignoredMemberId = null)
    {
        if (role != UserRole.Child)
        {
            return;
        }

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        var maxChildren = family.Plan?.MaxChildren ?? 0;
        var members = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var childCount = members.Count(member => member.Role == UserRole.Child && member.Id != ignoredMemberId);

        if (childCount >= maxChildren)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Plan_Limit_Exceeded, cancellationToken));
        }
    }

    private async Task EnsureFamilyAdminRemovalAllowedAsync(Guid familyId, FamilyMember member, CancellationToken cancellationToken)
    {
        if (member.Role != UserRole.FamilyAdmin)
        {
            return;
        }

        var familyAdminCount = await _familyMemberRepository.CountActiveByRoleAsync(familyId, UserRole.FamilyAdmin, cancellationToken);

        if (familyAdminCount <= 1)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<User> GetOrCreateMemberUserAsync(string phoneNumber, string fullName, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (user is not null)
        {
            user.FullName = fullName;
            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        user = new User
        {
            PhoneNumber = phoneNumber,
            CountryCode = ExtractCountryCode(phoneNumber),
            FullName = fullName,
            IsActive = true,
            PreferredLanguage = "en"
        };

        await _userRepository.AddAsync(user, cancellationToken);

        return user;
    }

    private async Task EnsureRoleProfileAsync(FamilyMember familyMember, CancellationToken cancellationToken)
    {
        if (familyMember.Role == UserRole.Child
            && await _childProfileRepository.GetByFamilyMemberIdAsync(familyMember.Id, cancellationToken) is null)
        {
            await _childProfileRepository.AddAsync(
                new ChildProfile
                {
                    FamilyMemberId = familyMember.InternalId,
                    FamilyId = familyMember.FamilyId,
                    UserId = familyMember.UserId,
                    AvatarCode = "avatar_01",
                    LevelCode = 1
                },
                cancellationToken);
        }

        if (familyMember.Role == UserRole.Teacher
            && await _teacherProfileRepository.GetByFamilyMemberIdAsync(familyMember.Id, cancellationToken) is null)
        {
            await _teacherProfileRepository.AddAsync(
                new TeacherProfile
                {
                    FamilyMemberId = familyMember.InternalId,
                    FamilyId = familyMember.FamilyId,
                    UserId = familyMember.UserId,
                    SubjectName = "General",
                    TeacherType = "Other",
                    IsActive = true
                },
                cancellationToken);
        }
    }

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        var random = Random.Shared;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var joinCode = new string(Enumerable.Range(0, JoinCodeLength)
                .Select(_ => JoinCodeCharacters[random.Next(JoinCodeCharacters.Length)])
                .ToArray());

            if (!await _familyRepository.ExistsByJoinCodeAsync(joinCode, cancellationToken))
            {
                return joinCode;
            }
        }

        throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Technical_Error, cancellationToken));
    }

    private static Family CreateFamily(CreateFamilyRequest request, Guid currentUserId, Plan plan, string joinCode)
    {
        return new Family
        {
            FamilyName = request.FamilyName.Trim(),
            JoinCode = joinCode,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            PlanId = plan.InternalId,
            FamilyAdminUserId = 0L // Will be resolved via User entity after save — set to 0 as placeholder
        };
    }

    private static Subscription CreateTrialSubscription(Guid familyId, Plan plan)
    {
        var startDate = DateTime.UtcNow;

        return new Subscription
        {
            FamilyId = 0L, // long FK — will be set via family graph save
            PlanId = plan.InternalId,
            Status = "Trial",
            StartDate = startDate,
            TrialEndDate = startDate.AddDays(plan.TrialDays)
        };
    }

    private static FamilyMember CreateFamilyAdminMember(Guid familyId, Guid userId)
    {
        return CreateFamilyMember(familyId, userId, UserRole.FamilyAdmin, "Father", null, null);
    }

    private static FamilyMember CreateFamilyMember(Guid familyId, Guid userId, UserRole role, string linkType, string? displayName, Guid? invitedByUserId)
    {
        return new FamilyMember
        {
            FamilyId = 0L, // long FK — will be set via family graph save
            UserId = 0L,   // long FK — will be resolved after User save
            Role = role,
            LinkType = linkType.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            InvitedByUserId = null // long? FK — Guid? not directly convertible
        };
    }

    private async Task EnsureAssignableRoleAsync(UserRole role, CancellationToken cancellationToken)
    {
        if (role == UserRole.SuperAdmin || !Enum.IsDefined(role))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Role, cancellationToken));
        }
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Family,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await _errorCodeService.GetMessageAsync(
            FamilyFirstErrorCode.Invalid_MasterData,
            cancellationToken: cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(MasterDataCodes)] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }

    private static FamilyDto ToFamilyDto(Family family)
    {
        return ToFamilyDto(family, family.Plan?.PlanCode ?? string.Empty);
    }

    private static FamilyDto ToFamilyDto(Family family, string planCode)
    {
        return new FamilyDto(
            family.Id,
            family.FamilyName,
            family.JoinCode,
            family.City,
            (int)family.PlanId,
            planCode,
            family.Subscription?.Id,
            family.FamilyAdminUser?.Id ?? Guid.Empty,
            family.FamilyScore,
            family.CurrentStreakDays,
            family.BestStreakDays,
            family.TimezoneId,
            family.IsActive);
    }

    private static FamilyMemberDto ToFamilyMemberDto(FamilyMember member)
    {
        return ToFamilyMemberDto(member, member.User);
    }

    private static FamilyMemberDto ToFamilyMemberDto(FamilyMember member, User? user)
    {
        return new FamilyMemberDto(
            member.Id,
            member.Family?.Id ?? Guid.Empty,
            member.User?.Id ?? user?.Id ?? Guid.Empty,
            member.Role,
            member.LinkType,
            member.DisplayName,
            user?.FullName ?? member.User?.FullName ?? string.Empty,
            user?.PhoneNumber ?? member.User?.PhoneNumber ?? string.Empty,
            member.IsActive,
            member.JoinedAt);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();

        return trimmed.StartsWith('+') ? trimmed : $"+91{trimmed}";
    }

    private static string ExtractCountryCode(string phoneNumber)
    {
        return phoneNumber.StartsWith("+91", StringComparison.Ordinal) ? "+91" : phoneNumber[..Math.Min(3, phoneNumber.Length)];
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
        {
            return phoneNumber;
        }

        return $"{phoneNumber[..Math.Min(4, phoneNumber.Length)]}****{phoneNumber[^2..]}";
    }
}
