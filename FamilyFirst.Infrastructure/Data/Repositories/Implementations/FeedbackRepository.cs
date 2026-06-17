using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public FeedbackRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TeacherFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        return QueryFeedback()
            .SingleOrDefaultAsync(feedback => feedback.Id == feedbackId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeacherFeedback>> ListByFamilyAsync(
        Guid familyId,
        Guid? teacherProfileId,
        Guid? childProfileId,
        FeedbackType? feedbackType,
        bool? isAcknowledged,
        CancellationToken cancellationToken)
    {
        var query = QueryFeedback()
            .Where(feedback => feedback.Family!.Id == familyId);

        if (teacherProfileId.HasValue)
        {
            query = query.Where(feedback => feedback.TeacherProfile!.Id == teacherProfileId.Value);
        }

        if (childProfileId.HasValue)
        {
            query = query.Where(feedback => feedback.ChildProfile!.Id == childProfileId.Value);
        }

        if (feedbackType.HasValue)
        {
            query = query.Where(feedback => feedback.FeedbackType == feedbackType.Value);
        }

        if (isAcknowledged.HasValue)
        {
            query = query.Where(feedback => feedback.IsAcknowledged == isAcknowledged.Value);
        }

        return await query
            .OrderByDescending(feedback => feedback.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeacherFeedback>> ListByChildSinceAsync(
        Guid familyId,
        Guid childProfileId,
        DateTime createdFromUtc,
        CancellationToken cancellationToken)
    {
        return await QueryFeedback()
            .Where(feedback =>
                feedback.Family!.Id == familyId
                && feedback.ChildProfile!.Id == childProfileId
                && feedback.CreatedAt >= createdFromUtc)
            .OrderByDescending(feedback => feedback.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountUnacknowledgedByFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<TeacherFeedback>()
            .CountAsync(feedback => feedback.Family!.Id == familyId && !feedback.IsAcknowledged, cancellationToken);
    }

    public async Task AddAsync(TeacherFeedback feedback, CancellationToken cancellationToken)
    {
        await _dbContext.Set<TeacherFeedback>().AddAsync(feedback, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TeacherFeedback feedback, CancellationToken cancellationToken)
    {
        _dbContext.Set<TeacherFeedback>().Update(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TeacherFeedback> QueryFeedback()
    {
        return _dbContext.Set<TeacherFeedback>()
            .Include(feedback => feedback.TeacherProfile)
            .ThenInclude(profile => profile!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(feedback => feedback.TeacherProfile)
            .ThenInclude(profile => profile!.User)
            .Include(feedback => feedback.ChildProfile)
            .ThenInclude(child => child!.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(feedback => feedback.ChildProfile)
            .ThenInclude(child => child!.User)
            .Include(feedback => feedback.CommentTemplate)
            .Include(feedback => feedback.Session)
            .Include(feedback => feedback.AcknowledgedByUser);
    }
}
