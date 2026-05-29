using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class CommentTemplateRepository : ICommentTemplateRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public CommentTemplateRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CommentTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
    {
        return _dbContext.CommentTemplates
            .SingleOrDefaultAsync(template => template.TemplateId == templateId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CommentTemplate>> ListVisibleByFamilyAsync(
        Guid familyId,
        string? category,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CommentTemplates
            .Where(template => template.IsSystem || template.FamilyId == familyId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(template => template.Category == category);
        }

        return await query
            .OrderBy(template => template.SortOrder)
            .ThenBy(template => template.TemplateText)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountActiveFamilyTemplatesByCategoryAsync(
        Guid familyId,
        string category,
        Guid? excludedTemplateId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CommentTemplates.CountAsync(
            template => template.FamilyId == familyId
                && !template.IsSystem
                && template.Category == category
                && (!excludedTemplateId.HasValue || template.TemplateId != excludedTemplateId.Value),
            cancellationToken);
    }

    public async Task<int> GetNextSortOrderAsync(Guid familyId, string category, CancellationToken cancellationToken)
    {
        var currentMaxSortOrder = await _dbContext.CommentTemplates
            .Where(template =>
                template.FamilyId == familyId
                && !template.IsSystem
                && template.Category == category)
            .Select(template => (int?)template.SortOrder)
            .MaxAsync(cancellationToken);

        return (currentMaxSortOrder ?? 0) + 10;
    }

    public async Task AddAsync(CommentTemplate commentTemplate, CancellationToken cancellationToken)
    {
        await _dbContext.CommentTemplates.AddAsync(commentTemplate, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CommentTemplate commentTemplate, CancellationToken cancellationToken)
    {
        _dbContext.CommentTemplates.Update(commentTemplate);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
