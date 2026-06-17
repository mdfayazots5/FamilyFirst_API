using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class TaskCompletionRepository : ITaskCompletionRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public TaskCompletionRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TaskCompletion?> GetByIdAsync(Guid completionId, CancellationToken cancellationToken)
    {
        return QueryTaskCompletions()
            .SingleOrDefaultAsync(taskCompletion => taskCompletion.Id == completionId, cancellationToken);
    }

    public Task<TaskCompletion?> GetByTaskChildAndDateAsync(
        Guid taskId,
        Guid childProfileId,
        DateOnly scheduledDate,
        CancellationToken cancellationToken)
    {
        return QueryTaskCompletions()
            .SingleOrDefaultAsync(
                taskCompletion => taskCompletion.TaskItem!.Id == taskId
                    && taskCompletion.ChildProfile!.Id == childProfileId
                    && DateOnly.FromDateTime(taskCompletion.ScheduledDate) == scheduledDate,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskCompletion>> ListByFamilyAsync(
        Guid familyId,
        Guid? childProfileId,
        DateOnly? scheduledDate,
        CancellationToken cancellationToken)
    {
        var query = QueryTaskCompletions()
            .Where(taskCompletion => taskCompletion.Family!.Id == familyId);

        if (childProfileId.HasValue)
        {
            query = query.Where(taskCompletion => taskCompletion.ChildProfile!.Id == childProfileId.Value);
        }

        if (scheduledDate.HasValue)
        {
            query = query.Where(taskCompletion => DateOnly.FromDateTime(taskCompletion.ScheduledDate) == scheduledDate.Value);
        }

        return await query
            .OrderByDescending(taskCompletion => taskCompletion.ScheduledDate)
            .ThenByDescending(taskCompletion => taskCompletion.SubmittedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskCompletion>> ListPendingVerificationAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await QueryTaskCompletions()
            .Where(taskCompletion =>
                taskCompletion.Family!.Id == familyId
                && taskCompletion.Status == TaskStatus.SubmittedForReview)
            .OrderBy(taskCompletion => taskCompletion.SubmittedAt)
            .ThenBy(taskCompletion => taskCompletion.ChildProfile!.FamilyMember!.User!.FullName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(TaskCompletion taskCompletion, CancellationToken cancellationToken)
    {
        await _dbContext.TaskCompletions.AddAsync(taskCompletion, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskCompletion taskCompletion, CancellationToken cancellationToken)
    {
        _dbContext.TaskCompletions.Update(taskCompletion);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWithChildProfileAsync(
        TaskCompletion taskCompletion,
        ChildProfile childProfile,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.TaskCompletions.Update(taskCompletion);
        _dbContext.ChildProfiles.Update(childProfile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<TaskCompletion> QueryTaskCompletions()
    {
        return _dbContext.TaskCompletions
            .Include(taskCompletion => taskCompletion.TaskItem)
            .Include(taskCompletion => taskCompletion.ChildProfile)
            .ThenInclude(childProfile => childProfile!.FamilyMember)
            .ThenInclude(familyMember => familyMember!.User)
            .Include(taskCompletion => taskCompletion.ChildProfile)
            .ThenInclude(childProfile => childProfile!.User)
            .Include(taskCompletion => taskCompletion.ReviewedByUser)
            .Include(taskCompletion => taskCompletion.Family);
    }
}
