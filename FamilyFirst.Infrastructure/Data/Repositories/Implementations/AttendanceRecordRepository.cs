using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class AttendanceRecordRepository : IAttendanceRecordRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public AttendanceRecordRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AttendanceRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken)
    {
        return QueryRecords()
            .SingleOrDefaultAsync(record => record.Id == recordId, cancellationToken);
    }

    public Task<AttendanceRecord?> GetByIdForFamilyAsync(Guid id, long familyId, CancellationToken cancellationToken)
    {
        return QueryRecords()
            .SingleOrDefaultAsync(record => record.Id == id && record.FamilyId == familyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceRecord>> ListBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return await QueryRecords()
            .Where(record => record.AttendanceSession!.Id == sessionId)
            .OrderBy(record => record.ChildProfile!.FamilyMember!.User!.FullName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceRecord>> ListByChildAndDateRangeAsync(
        Guid familyId,
        Guid childProfileId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = QueryRecords()
            .Where(record => record.Family!.Id == familyId && record.ChildProfile!.Id == childProfileId);

        if (fromDate.HasValue)
        {
            query = query.Where(record => DateOnly.FromDateTime(record.AttendanceSession!.ScheduledDate) >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(record => DateOnly.FromDateTime(record.AttendanceSession!.ScheduledDate) <= toDate.Value);
        }

        return await query
            .OrderByDescending(record => record.AttendanceSession!.ScheduledDate)
            .ThenBy(record => record.AttendanceSession!.StartTime)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddSubmissionAsync(
        AttendanceSession session,
        IReadOnlyCollection<AttendanceRecord> records,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.AttendanceSessions.Update(session);
        await _dbContext.AttendanceRecords.AddRangeAsync(records, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken)
    {
        _dbContext.AttendanceRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AttendanceRecord> QueryRecords()
    {
        return _dbContext.AttendanceRecords
            .Include(record => record.AttendanceSession)
            .ThenInclude(session => session!.TeacherProfile)
            .ThenInclude(profile => profile!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(record => record.ChildProfile)
            .ThenInclude(child => child!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(record => record.ChildProfile)
            .ThenInclude(child => child!.User)
            .Include(record => record.MarkedByUser)
            .Include(record => record.EditedByUser);
    }
}
