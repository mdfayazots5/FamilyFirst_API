using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class AttendanceSessionRepository : IAttendanceSessionRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public AttendanceSessionRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AttendanceSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return QuerySessions()
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceSession>> ListByTeacherAndDateAsync(
        Guid teacherProfileId,
        DateOnly scheduledDate,
        CancellationToken cancellationToken)
    {
        return await QuerySessions()
            .Where(session =>
                session.TeacherProfileId == teacherProfileId
                && session.ScheduledDate == scheduledDate
                && session.IsActive)
            .OrderBy(session => session.StartTime)
            .ThenBy(session => session.SessionName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceSession>> ListByAssignedChildrenAndDateAsync(
        Guid familyId,
        IReadOnlyCollection<Guid> childProfileIds,
        DateOnly scheduledDate,
        CancellationToken cancellationToken)
    {
        if (childProfileIds.Count == 0)
        {
            return Array.Empty<AttendanceSession>();
        }

        return await QuerySessions()
            .Where(session =>
                session.FamilyId == familyId
                && session.ScheduledDate == scheduledDate
                && session.IsActive
                && _dbContext.TeacherChildAssignments.Any(assignment =>
                    assignment.FamilyId == familyId
                    && assignment.TeacherProfileId == session.TeacherProfileId
                    && assignment.IsActive
                    && childProfileIds.Contains(assignment.ChildProfileId)))
            .OrderBy(session => session.StartTime)
            .ThenBy(session => session.SessionName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(AttendanceSession session, CancellationToken cancellationToken)
    {
        await _dbContext.AttendanceSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AttendanceSession session, CancellationToken cancellationToken)
    {
        _dbContext.AttendanceSessions.Update(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AttendanceSession> QuerySessions()
    {
        return _dbContext.AttendanceSessions
            .Include(session => session.TeacherProfile)
            .ThenInclude(profile => profile!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(session => session.TeacherProfile)
            .ThenInclude(profile => profile!.User);
    }
}
