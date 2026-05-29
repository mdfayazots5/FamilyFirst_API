using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class TeacherService : ITeacherService
{
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly ITeacherChildAssignmentRepository _teacherChildAssignmentRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;

    public TeacherService(
        ITeacherProfileRepository teacherProfileRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IFamilyMemberRepository familyMemberRepository)
    {
        _teacherProfileRepository = teacherProfileRepository;
        _childProfileRepository = childProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<bool> AssignTeacherAsync(
        Guid currentUserId,
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var teacher = await GetTeacherInFamilyOrThrowAsync(teacherId, familyId, cancellationToken);
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);

        var existingAssignment = await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
            teacher.Id,
            child.Id,
            cancellationToken);

        if (existingAssignment is not null)
        {
            throw new ConflictException("Teacher is already assigned to this child.");
        }

        await _teacherChildAssignmentRepository.AddAsync(
            new TeacherChildAssignment
            {
                TeacherProfileId = teacher.Id,
                ChildProfileId = child.Id,
                FamilyId = familyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            },
            cancellationToken);

        return true;
    }

    public async Task<bool> UnassignTeacherAsync(
        Guid currentUserId,
        Guid familyId,
        Guid teacherId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var teacher = await GetTeacherInFamilyOrThrowAsync(teacherId, familyId, cancellationToken);
        var child = await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var assignment = await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
            teacher.Id,
            child.Id,
            cancellationToken)
            ?? throw new NotFoundException("Teacher assignment was not found.");

        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.DeletedAt = DateTime.UtcNow;

        await _teacherChildAssignmentRepository.UpdateAsync(assignment, cancellationToken);

        return true;
    }

    private async Task<TeacherProfile> GetTeacherInFamilyOrThrowAsync(Guid teacherId, Guid familyId, CancellationToken cancellationToken)
    {
        var teacher = await _teacherProfileRepository.GetByIdAsync(teacherId, cancellationToken);

        if (teacher is null || teacher.FamilyId != familyId || !teacher.IsActive)
        {
            throw new NotFoundException(nameof(TeacherProfile), teacherId);
        }

        return teacher;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childId, Guid familyId, CancellationToken cancellationToken)
    {
        var child = await _childProfileRepository.GetByIdAsync(childId, cancellationToken);

        if (child is null || child.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childId);
        }

        return child;
    }

    private async Task EnsureFamilyAdminAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("FamilyAdmin role is required.");
        }
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }
}
