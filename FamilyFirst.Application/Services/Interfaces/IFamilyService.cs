using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IFamilyService
{
    Task<FamilyDto> CreateFamilyAsync(Guid currentUserId, CreateFamilyRequest request, CancellationToken cancellationToken);

    Task<FamilyDto> GetFamilyAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<FamilyDto> UpdateFamilyAsync(Guid currentUserId, Guid familyId, UpdateFamilyRequest request, CancellationToken cancellationToken);

    Task<string> GetJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<string> RegenerateJoinCodeAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<FamilyMemberDto> JoinFamilyAsync(Guid currentUserId, JoinFamilyRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FamilyMemberDto>> ListMembersAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<FamilyMemberDto> AddMemberAsync(Guid currentUserId, Guid familyId, AddMemberRequest request, CancellationToken cancellationToken);

    Task<FamilyMemberDto> UpdateMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken);

    Task<bool> RemoveMemberAsync(Guid currentUserId, Guid familyId, Guid memberId, CancellationToken cancellationToken);

    Task<FamilyDashboardDto> GetDashboardAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);
}

public interface IFamilyRepository
{
    Task<Plan?> GetPlanByCodeAsync(string planCode, CancellationToken cancellationToken);

    Task<Plan?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken);

    Task<Family?> GetByIdAsync(Guid familyId, CancellationToken cancellationToken);

    Task<Family?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);

    Task<bool> ExistsByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);

    Task<bool> UserOwnsActiveFamilyAsync(Guid userId, CancellationToken cancellationToken);

    Task AddFamilyGraphAsync(Family family, Subscription subscription, FamilyMember familyMember, CancellationToken cancellationToken);

    Task UpdateAsync(Family family, CancellationToken cancellationToken);
}

public interface IFamilyMemberRepository
{
    Task<FamilyMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken);

    Task<FamilyMember?> GetActiveByFamilyAndUserAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);

    Task<FamilyMember?> GetPrimaryActiveMembershipForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FamilyMember>> ListActiveByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<int> CountActiveByRoleAsync(Guid familyId, UserRole role, CancellationToken cancellationToken);

    Task AddAsync(FamilyMember familyMember, CancellationToken cancellationToken);

    Task UpdateAsync(FamilyMember familyMember, CancellationToken cancellationToken);
}

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken);
}
