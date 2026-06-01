using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using TaskDeductCoinsRequest = FamilyFirst.Application.DTOs.Task.DeductCoinsRequest;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class ChildService : IChildService
{
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ICoinService _coinService;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public ChildService(
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        IFamilyMemberRepository familyMemberRepository)
    {
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<IReadOnlyCollection<ChildSummaryDto>> ListChildrenAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyRoleAsync(currentUserId, familyId, cancellationToken, UserRole.Parent, UserRole.FamilyAdmin);

        var children = await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken);

        return children.Select(ToSummaryDto).ToArray();
    }

    public async Task<ChildDetailDto> GetChildAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is UserRole.Parent or UserRole.FamilyAdmin)
        {
            return ToDetailDto(child);
        }

        if (member.Role == UserRole.Child && currentChildProfileId == child.Id)
        {
            return ToDetailDto(child);
        }

        throw new ForbiddenAccessException("Child profile access is not allowed.");
    }

    public async Task<ChildDetailDto> UpdateChildAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        UpdateChildRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyRoleAsync(currentUserId, familyId, cancellationToken, UserRole.Parent, UserRole.FamilyAdmin);

        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        child.DateOfBirth = request.DateOfBirth.HasValue ? request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
        child.GradeLevel = string.IsNullOrWhiteSpace(request.GradeLevel) ? null : request.GradeLevel.Trim();
        child.SchoolName = string.IsNullOrWhiteSpace(request.SchoolName) ? null : request.SchoolName.Trim();
        child.AvatarCode = request.AvatarCode.Trim();

        await _childProfileRepository.UpdateAsync(child, cancellationToken);

        return ToDetailDto(child);
    }

    public async Task<IReadOnlyCollection<ScoreHistoryDto>> GetScoreHistoryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyRoleAsync(currentUserId, familyId, cancellationToken, UserRole.Parent, UserRole.FamilyAdmin);

        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var scoreDate = DateOnly.FromDateTime(child.ScoreUpdatedAt ?? child.LastUpdated ?? child.DateCreated);

        return new[]
        {
            new ScoreHistoryDto(
                child.Id,
                scoreDate,
                child.StudyScore,
                child.CleanlinessScore,
                child.DisciplineScore,
                child.ScreenControlScore,
                child.ResponsibilityScore)
        };
    }

    public Task<PaginatedList<CoinTransactionDto>> GetCoinHistoryAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return _coinService.GetHistoryAsync(
            currentUserId,
            currentChildProfileId,
            familyId,
            childId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<CoinTransactionDto> DeductCoinsAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        TaskDeductCoinsRequest request,
        CancellationToken cancellationToken)
    {
        return _coinService.DeductCoinsAsync(
            currentUserId,
            familyId,
            childId,
            request,
            cancellationToken);
    }

    public Task<bool> UseStreakFreezeAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        return _coinService.UseStreakFreezeAsync(
            currentUserId,
            currentChildProfileId,
            familyId,
            childId,
            cancellationToken);
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childId, Guid familyId, CancellationToken cancellationToken)
    {
        var child = await _childProfileRepository.GetByIdAsync(childId, cancellationToken);

        if (child is null || child.Family?.Id != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childId);
        }

        return child;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task<FamilyMember> EnsureFamilyRoleAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken,
        params UserRole[] allowedRoles)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (!allowedRoles.Contains(member.Role))
        {
            throw new ForbiddenAccessException("User role is not allowed for this child profile operation.");
        }

        return member;
    }

    private static ChildSummaryDto ToSummaryDto(ChildProfile child)
    {
        return new ChildSummaryDto(
            child.Id,
            child.FamilyMember?.Id ?? Guid.Empty,
            child.User?.Id ?? Guid.Empty,
            child.Family?.Id ?? Guid.Empty,
            child.FamilyMember?.User?.FullName ?? child.User?.FullName ?? string.Empty,
            child.GradeLevel,
            child.SchoolName,
            child.AvatarCode,
            child.CoinBalance,
            child.TotalCoinsEarned,
            child.CurrentStreakDays,
            child.BestStreakDays,
            child.LevelCode,
            child.StudyScore,
            child.CleanlinessScore,
            child.DisciplineScore,
            child.ScreenControlScore,
            child.ResponsibilityScore);
    }

    private static ChildDetailDto ToDetailDto(ChildProfile child)
    {
        return new ChildDetailDto(
            child.Id,
            child.FamilyMember?.Id ?? Guid.Empty,
            child.User?.Id ?? Guid.Empty,
            child.Family?.Id ?? Guid.Empty,
            child.FamilyMember?.User?.FullName ?? child.User?.FullName ?? string.Empty,
            child.DateOfBirth.HasValue ? DateOnly.FromDateTime(child.DateOfBirth.Value) : (DateOnly?)null,
            CalculateAgeYears(child.DateOfBirth.HasValue ? DateOnly.FromDateTime(child.DateOfBirth.Value) : (DateOnly?)null),
            child.GradeLevel,
            child.SchoolName,
            child.AvatarCode,
            child.CoinBalance,
            child.TotalCoinsEarned,
            child.CurrentStreakDays,
            child.BestStreakDays,
            child.StreakFreezesAvailable,
            child.LevelCode,
            child.StudyScore,
            child.CleanlinessScore,
            child.DisciplineScore,
            child.ScreenControlScore,
            child.ResponsibilityScore,
            child.ScoreUpdatedAt);
    }

    private static int? CalculateAgeYears(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ageYears = today.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value > today.AddYears(-ageYears))
        {
            ageYears--;
        }

        return ageYears;
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }
}
