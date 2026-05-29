using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ITeacherService
{
    Task<bool> AssignTeacherAsync(Guid currentUserId, Guid familyId, Guid teacherId, Guid childId, CancellationToken cancellationToken);

    Task<bool> UnassignTeacherAsync(Guid currentUserId, Guid familyId, Guid teacherId, Guid childId, CancellationToken cancellationToken);
}

public interface ITeacherProfileRepository
{
    Task<TeacherProfile?> GetByIdAsync(Guid teacherProfileId, CancellationToken cancellationToken);

    Task<TeacherProfile?> GetByFamilyMemberIdAsync(Guid familyMemberId, CancellationToken cancellationToken);

    Task AddAsync(TeacherProfile teacherProfile, CancellationToken cancellationToken);
}

public interface ITeacherChildAssignmentRepository
{
    Task<TeacherChildAssignment?> GetActiveByTeacherAndChildAsync(Guid teacherProfileId, Guid childProfileId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> ListActiveChildIdsByTeacherProfileIdAsync(Guid teacherProfileId, CancellationToken cancellationToken);

    Task AddAsync(TeacherChildAssignment assignment, CancellationToken cancellationToken);

    Task UpdateAsync(TeacherChildAssignment assignment, CancellationToken cancellationToken);
}
