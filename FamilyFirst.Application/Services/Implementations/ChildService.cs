using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Family;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;
using TaskDeductCoinsRequest = FamilyFirst.Application.DTOs.Task.DeductCoinsRequest;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class ChildService : IChildService
{
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ICoinService _coinService;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public ChildService(
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        IFamilyMemberRepository familyMemberRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _familyMemberRepository = familyMemberRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<IReadOnlyCollection<ChildSummaryDto>> ListChildrenAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyRoleAsync(currentUserId, familyId, cancellationToken, UserRole.Parent, UserRole.FamilyAdmin);

        var children = await _childProfileRepository.ListByFamilyAsync(familyId, cancellationToken);
        var response = children.Select(ToSummaryDto).ToArray();
        LogApiCall(nameof(ListChildrenAsync), new { currentUserId, familyId }, new { Count = response.Length });
        return response;
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
            var response = ToDetailDto(child);
            LogApiCall(nameof(GetChildAsync), new { currentUserId, familyId, childId }, new { response.ChildProfileId });
            return response;
        }

        if (member.Role == UserRole.Child && currentChildProfileId == child.Id)
        {
            var response = ToDetailDto(child);
            LogApiCall(nameof(GetChildAsync), new { currentUserId, familyId, childId }, new { response.ChildProfileId });
            return response;
        }

        throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    public async Task<ChildDetailDto> UpdateChildAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        UpdateChildRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyRoleAsync(currentUserId, familyId, cancellationToken, UserRole.Parent, UserRole.FamilyAdmin);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        child.DateOfBirth = request.DateOfBirth.HasValue ? request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
        child.GradeLevel = string.IsNullOrWhiteSpace(request.GradeLevel) ? null : request.GradeLevel.Trim();
        child.SchoolName = string.IsNullOrWhiteSpace(request.SchoolName) ? null : request.SchoolName.Trim();
        child.AvatarCode = request.AvatarCode.Trim();

        await _childProfileRepository.UpdateAsync(child, cancellationToken);

        var response = ToDetailDto(child);
        LogApiCall(nameof(UpdateChildAsync), new { currentUserId, familyId, childId, request.AvatarCode }, new { response.ChildProfileId });
        return response;
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

        var response = new[]
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
        LogApiCall(nameof(GetScoreHistoryAsync), new { currentUserId, familyId, childId }, new { Count = response.Length });
        return response;
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
        LogApiCall(nameof(GetCoinHistoryAsync), new { currentUserId, familyId, childId, pageNumber, pageSize }, null);
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
        LogApiCall(nameof(DeductCoinsAsync), new { currentUserId, familyId, childId, request.Amount }, null);
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
        LogApiCall(nameof(UseStreakFreezeAsync), new { currentUserId, familyId, childId }, null);
        return _coinService.UseStreakFreezeAsync(
            currentUserId,
            currentChildProfileId,
            familyId,
            childId,
            cancellationToken);
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childId, Guid familyId, CancellationToken cancellationToken)
    {
        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile,
            childId.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedChildId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var child = await _childProfileRepository.GetByIdAsync(childId, cancellationToken);

        if (child is null || child.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return child;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
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

    private async Task<long> GetFamilyInternalIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return resolvedFamilyId.Value;
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
            FamilyFirstModule.Task,
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
}
