using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class TeacherChildAssignmentRepository : ITeacherChildAssignmentRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public TeacherChildAssignmentRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TeacherChildAssignment?> GetActiveByTeacherAndChildAsync(
        Guid teacherProfileId,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TeacherChildAssignments
            .SingleOrDefaultAsync(
                assignment =>
                    assignment.TeacherProfile!.Id == teacherProfileId
                    && assignment.ChildProfile!.Id == childProfileId
                    && assignment.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> ListActiveChildIdsByTeacherProfileIdAsync(
        Guid teacherProfileId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TeacherChildAssignments
            .Where(assignment => assignment.TeacherProfile!.Id == teacherProfileId && assignment.IsActive)
            .OrderBy(assignment => assignment.AssignedAt)
            .Select(assignment => assignment.ChildProfile!.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(TeacherChildAssignment assignment, CancellationToken cancellationToken)
    {
        await _dbContext.TeacherChildAssignments.AddAsync(assignment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TeacherChildAssignment assignment, CancellationToken cancellationToken)
    {
        _dbContext.TeacherChildAssignments.Update(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
