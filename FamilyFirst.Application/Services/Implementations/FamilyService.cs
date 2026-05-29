using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

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

    public FamilyService(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IFeedbackRepository feedbackRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherProfileRepository teacherProfileRepository,
        IUserRepository userRepository)
    {
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
        _feedbackRepository = feedbackRepository;
        _childProfileRepository = childProfileRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _userRepository = userRepository;
    }

    public async Task<FamilyDto> CreateFamilyAsync(Guid currentUserId, CreateFamilyRequest request, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        if (await _familyRepository.UserOwnsActiveFamilyAsync(currentUserId, cancellationToken))
        {
            throw new ConflictException("User already owns an active family.");
        }

        var plan = await _familyRepository.GetPlanByCodeAsync(FreeTrialPlanCode, cancellationToken)
            ?? throw new NotFoundException("Free trial plan was not found.");
        var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
        var family = CreateFamily(request, currentUserId, plan, joinCode);
        var subscription = CreateTrialSubscription(family.Id, plan);
        var familyMember = CreateFamilyAdminMember(family.Id, currentUserId);

        family.SubscriptionId = subscription.Id;
        await _familyRepository.AddFamilyGraphAsync(family, subscription, familyMember, cancellationToken);

        return ToFamilyDto(family, plan.PlanCode);
    }

    public async Task<FamilyDto> GetFamilyAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);

        return ToFamilyDto(family);
    }

    public async Task<FamilyDto> UpdateFamilyAsync(Guid currentUserId, Guid familyId, UpdateFamilyRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        family.FamilyName = request.FamilyName.Trim();
        family.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        await _familyRepository.UpdateAsync(family, cancellationToken);

        return ToFamilyDto(family);
    }

    public async Task<string> GetJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);

        return family.JoinCode;
    }

    public async Task<string> RegenerateJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        family.JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);

        await _familyRepository.UpdateAsync(family, cancellationToken);

        return family.JoinCode;
    }

    public async Task<FamilyMemberDto> JoinFamilyAsync(Guid currentUserId, JoinFamilyRequest request, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        EnsureAssignableRole(request.Role);

        var family = await _familyRepository.GetByJoinCodeAsync(request.JoinCode.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Family join code was not found.");

        if (await _familyMemberRepository.GetActiveByFamilyAndUserAsync(family.Id, currentUserId, cancellationToken) is not null)
        {
            throw new ConflictException("User is already a member of this family.");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), currentUserId);
        user.FullName = request.FullName.Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var familyMember = CreateFamilyMember(family.Id, currentUserId, request.Role, request.LinkType, null, null);
        await _familyMemberRepository.AddAsync(familyMember, cancellationToken);
        await EnsureRoleProfileAsync(familyMember, cancellationToken);

        return ToFamilyMemberDto(familyMember, user);
    }

    public async Task<IReadOnlyCollection<FamilyMemberDto>> ListMembersAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        var members = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);

        return members.Select(ToFamilyMemberDto).ToArray();
    }

    public async Task<FamilyMemberDto> AddMemberAsync(Guid currentUserId, Guid familyId, AddMemberRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        EnsureAssignableRole(request.Role);
        await EnsurePlanLimitAsync(familyId, request.Role, cancellationToken);

        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var user = await GetOrCreateMemberUserAsync(phoneNumber, request.FullName.Trim(), cancellationToken);

        if (await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, user.Id, cancellationToken) is not null)
        {
            throw new ConflictException("User is already a member of this family.");
        }

        var familyMember = CreateFamilyMember(familyId, user.Id, request.Role, request.LinkType, null, currentUserId);
        await _familyMemberRepository.AddAsync(familyMember, cancellationToken);
        await EnsureRoleProfileAsync(familyMember, cancellationToken);

        return ToFamilyMemberDto(familyMember, user);
    }

    public async Task<FamilyMemberDto> UpdateMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        EnsureAssignableRole(request.Role);
        await EnsurePlanLimitAsync(familyId, request.Role, cancellationToken, memberId);

        var member = await GetFamilyMemberOrThrowAsync(memberId, familyId, cancellationToken);
        member.Role = request.Role;
        member.LinkType = request.LinkType.Trim();
        member.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

        await _familyMemberRepository.UpdateAsync(member, cancellationToken);
        await EnsureRoleProfileAsync(member, cancellationToken);

        return ToFamilyMemberDto(member);
    }

    public async Task<bool> RemoveMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var member = await GetFamilyMemberOrThrowAsync(memberId, familyId, cancellationToken);
        await EnsureFamilyAdminRemovalAllowedAsync(familyId, member, cancellationToken);

        member.IsDeleted = true;
        member.IsActive = false;
        member.DeletedAt = DateTime.UtcNow;

        await _familyMemberRepository.UpdateAsync(member, cancellationToken);

        return true;
    }

    public async Task<FamilyDashboardDto> GetDashboardAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var currentMember = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (currentMember.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Only Parent or FamilyAdmin can view the family dashboard.");
        }

        var family = await GetFamilyOrThrowAsync(familyId, cancellationToken);
        var members = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var unacknowledgedFeedbackCount = await _feedbackRepository.CountUnacknowledgedByFamilyAsync(familyId, cancellationToken);

        return new FamilyDashboardDto(
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
    }

    private async Task<Family> GetFamilyOrThrowAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await _familyRepository.GetByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Family), familyId);
    }

    private async Task<FamilyMember> GetFamilyMemberOrThrowAsync(Guid memberId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await _familyMemberRepository.GetByIdAsync(memberId, cancellationToken);

        if (member is null || member.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(FamilyMember), memberId);
        }

        return member;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task EnsureFamilyAdminAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("FamilyAdmin role is required.");
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
            throw new ConflictException("Plan child limit reached.");
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
            throw new ConflictException("The sole FamilyAdmin cannot be removed.");
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
                    FamilyMemberId = familyMember.Id,
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
                    FamilyMemberId = familyMember.Id,
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

        throw new ConflictException("Unable to generate a unique join code.");
    }

    private static Family CreateFamily(CreateFamilyRequest request, Guid currentUserId, Plan plan, string joinCode)
    {
        return new Family
        {
            FamilyName = request.FamilyName.Trim(),
            JoinCode = joinCode,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            PlanId = plan.PlanId,
            FamilyAdminUserId = currentUserId
        };
    }

    private static Subscription CreateTrialSubscription(Guid familyId, Plan plan)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        return new Subscription
        {
            FamilyId = familyId,
            PlanId = plan.PlanId,
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
            FamilyId = familyId,
            UserId = userId,
            Role = role,
            LinkType = linkType.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            InvitedByUserId = invitedByUserId
        };
    }

    private static void EnsureAssignableRole(UserRole role)
    {
        if (role == UserRole.SuperAdmin || !Enum.IsDefined(role))
        {
            throw new ForbiddenAccessException("SuperAdmin cannot be assigned via API.");
        }
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
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
            family.PlanId,
            planCode,
            family.SubscriptionId,
            family.FamilyAdminUserId,
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
            member.FamilyId,
            member.UserId,
            member.Role,
            member.LinkType,
            member.DisplayName,
            user?.FullName ?? string.Empty,
            user?.PhoneNumber ?? string.Empty,
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
}
