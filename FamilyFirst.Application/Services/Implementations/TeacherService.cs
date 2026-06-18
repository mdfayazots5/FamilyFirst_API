using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class TeacherService : ITeacherService
{
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly ITeacherChildAssignmentRepository _teacherChildAssignmentRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public TeacherService(
        ITeacherProfileRepository teacherProfileRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IFamilyMemberRepository familyMemberRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _teacherProfileRepository = teacherProfileRepository;
        _childProfileRepository = childProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _familyMemberRepository = familyMemberRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<bool> AssignTeacherAsync(
        Guid currentUserId,
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var teacher = await GetTeacherInFamilyOrThrowAsync(teacherId, familyId, cancellationToken);
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);

        var existingAssignment = await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
            teacher.Id,
            child.Id,
            cancellationToken);

        if (existingAssignment is not null)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        await _teacherChildAssignmentRepository.AddAsync(
            new TeacherChildAssignment
            {
                TeacherProfileId = teacher.InternalId,
                ChildProfileId = child.InternalId,
                FamilyId = member.FamilyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            },
            cancellationToken);

        LogApiCall(nameof(AssignTeacherAsync), new { currentUserId, familyId, teacherId, childId }, new { Success = true });
        return true;
    }

    public async Task<bool> UnassignTeacherAsync(
        Guid currentUserId,
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);

        var teacher = await GetTeacherInFamilyOrThrowAsync(teacherId, familyId, cancellationToken);
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var assignment = await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
            teacher.Id,
            child.Id,
            cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.DateDeleted = DateTime.UtcNow;

        await _teacherChildAssignmentRepository.UpdateAsync(assignment, cancellationToken);

        LogApiCall(nameof(UnassignTeacherAsync), new { currentUserId, familyId, teacherId, childId }, new { Success = true });
        return true;
    }

    private async Task<TeacherProfile> GetTeacherInFamilyOrThrowAsync(Guid teacherId, Guid familyId, CancellationToken cancellationToken)
    {
        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedTeacherId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.TeacherProfile,
            teacherId.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedTeacherId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var teacher = await _teacherProfileRepository.GetByIdAsync(teacherId, cancellationToken);

        if (teacher is null || teacher.Family?.Id != familyId || !teacher.IsActive)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return teacher;
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

    private async Task<FamilyMember> EnsureFamilyAdminAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);
        await EnsureFamilyGuidValidAsync(familyId, cancellationToken);

        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
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

    private async Task EnsureFamilyGuidValidAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
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
            FamilyFirstModule.AdminConfiguration,
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
