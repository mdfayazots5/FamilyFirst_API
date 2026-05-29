using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ICommentTemplateService
{
    Task<IReadOnlyCollection<CommentTemplateDto>> ListTemplatesAsync(Guid currentUserId, Guid familyId, string? category, CancellationToken cancellationToken);

    Task<CommentTemplateDto> CreateTemplateAsync(Guid currentUserId, Guid familyId, CreateCommentTemplateRequest request, CancellationToken cancellationToken);

    Task<CommentTemplateDto> UpdateTemplateAsync(Guid currentUserId, Guid familyId, Guid templateId, UpdateCommentTemplateRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteTemplateAsync(Guid currentUserId, Guid familyId, Guid templateId, CancellationToken cancellationToken);
}

public interface ICommentTemplateRepository
{
    Task<CommentTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CommentTemplate>> ListVisibleByFamilyAsync(Guid familyId, string? category, CancellationToken cancellationToken);

    Task<int> CountActiveFamilyTemplatesByCategoryAsync(Guid familyId, string category, Guid? excludedTemplateId, CancellationToken cancellationToken);

    Task<int> GetNextSortOrderAsync(Guid familyId, string category, CancellationToken cancellationToken);

    Task AddAsync(CommentTemplate commentTemplate, CancellationToken cancellationToken);

    Task UpdateAsync(CommentTemplate commentTemplate, CancellationToken cancellationToken);
}
