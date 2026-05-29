using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class TaskItemRepository : ITaskItemRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public TaskItemRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return QueryTaskItems()
            .SingleOrDefaultAsync(taskItem => taskItem.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListFamilyTasksAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await QueryTaskItems()
            .Where(taskItem => !taskItem.IsSystemTemplate && taskItem.FamilyId == familyId)
            .OrderBy(taskItem => taskItem.TimeBlock)
            .ThenBy(taskItem => taskItem.TaskName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListSystemTemplatesAsync(
        string? category,
        string? ageGroup,
        CancellationToken cancellationToken)
    {
        var query = QueryTaskItems()
            .Where(taskItem => taskItem.IsSystemTemplate);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(taskItem => taskItem.TemplateCategory == category);
        }

        if (!string.IsNullOrWhiteSpace(ageGroup))
        {
            query = query.Where(taskItem => taskItem.AgeGroup == ageGroup);
        }

        return await query
            .OrderBy(taskItem => taskItem.TemplateCategory)
            .ThenBy(taskItem => taskItem.AgeGroup)
            .ThenBy(taskItem => taskItem.TaskName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken)
    {
        await _dbContext.TaskItems.AddAsync(taskItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken)
    {
        _dbContext.TaskItems.Update(taskItem);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TaskItem> QueryTaskItems()
    {
        return _dbContext.TaskItems
            .Include(taskItem => taskItem.ChildProfile)
            .ThenInclude(childProfile => childProfile!.FamilyMember)
            .ThenInclude(familyMember => familyMember!.User)
            .Include(taskItem => taskItem.CreatedByUser)
            .Include(taskItem => taskItem.Family);
    }
}
