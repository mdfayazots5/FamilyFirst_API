using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class CommentTemplateService : ICommentTemplateService
{
    private const int FamilyTemplateLimitPerCategory = 20;
    private const int UnprocessableEntityStatusCode = 422;

    private readonly ICommentTemplateRepository _commentTemplateRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public CommentTemplateService(
        ICommentTemplateRepository commentTemplateRepository,
        IFamilyMemberRepository familyMemberRepository)
    {
        _commentTemplateRepository = commentTemplateRepository;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<IReadOnlyCollection<CommentTemplateDto>> ListTemplatesAsync(
        Guid currentUserId,
        Guid familyId,
        string? category,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Teacher and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Teacher or FamilyAdmin role is required.");
        }

        var normalizedCategory = NormalizeOptionalCategory(category);
        var templates = await _commentTemplateRepository.ListVisibleByFamilyAsync(
            familyId,
            normalizedCategory,
            cancellationToken);

        return templates
            .Select(template => new CommentTemplateDto(
                template.TemplateId,
                template.FamilyId,
                template.TemplateText,
                template.Category,
                template.IsSystem,
                template.SortOrder))
            .ToArray();
    }

    public async Task<CommentTemplateDto> CreateTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        CreateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var normalizedCategory = NormalizeRequiredCategory(request.Category);
        await EnsureFamilyTemplateLimitAsync(familyId, normalizedCategory, null, cancellationToken);

        var commentTemplate = new CommentTemplate
        {
            TemplateId = Guid.NewGuid(),
            FamilyId = familyId,
            TemplateText = request.TemplateText.Trim(),
            Category = normalizedCategory,
            IsSystem = false,
            IsActive = true,
            SortOrder = await _commentTemplateRepository.GetNextSortOrderAsync(
                familyId,
                normalizedCategory,
                cancellationToken)
        };

        await _commentTemplateRepository.AddAsync(commentTemplate, cancellationToken);

        return ToDto(commentTemplate);
    }

    public async Task<CommentTemplateDto> UpdateTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        Guid templateId,
        UpdateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var commentTemplate = await GetFamilyTemplateOrThrowAsync(templateId, familyId, cancellationToken);
        var normalizedCategory = NormalizeRequiredCategory(request.Category);
        await EnsureFamilyTemplateLimitAsync(familyId, normalizedCategory, templateId, cancellationToken);

        var categoryChanged = !string.Equals(commentTemplate.Category, normalizedCategory, StringComparison.Ordinal);
        commentTemplate.TemplateText = request.TemplateText.Trim();
        commentTemplate.Category = normalizedCategory;

        if (categoryChanged)
        {
            commentTemplate.SortOrder = await _commentTemplateRepository.GetNextSortOrderAsync(
                familyId,
                normalizedCategory,
                cancellationToken);
        }

        await _commentTemplateRepository.UpdateAsync(commentTemplate, cancellationToken);

        return ToDto(commentTemplate);
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var commentTemplate = await GetFamilyTemplateOrThrowAsync(templateId, familyId, cancellationToken);
        commentTemplate.IsActive = false;

        await _commentTemplateRepository.UpdateAsync(commentTemplate, cancellationToken);

        return true;
    }

    private async Task<CommentTemplate> GetFamilyTemplateOrThrowAsync(
        Guid templateId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var commentTemplate = await _commentTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new NotFoundException(nameof(CommentTemplate), templateId);

        if (commentTemplate.IsSystem)
        {
            throw new ForbiddenAccessException("System comment templates are read-only.");
        }

        if (commentTemplate.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(CommentTemplate), templateId);
        }

        return commentTemplate;
    }

    private async Task EnsureFamilyTemplateLimitAsync(
        Guid familyId,
        string category,
        Guid? excludedTemplateId,
        CancellationToken cancellationToken)
    {
        var existingCount = await _commentTemplateRepository.CountActiveFamilyTemplatesByCategoryAsync(
            familyId,
            category,
            excludedTemplateId,
            cancellationToken);

        if (existingCount >= FamilyTemplateLimitPerCategory)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Category"] = new[] { $"A family can have at most {FamilyTemplateLimitPerCategory} templates in the {category} category." }
                },
                UnprocessableEntityStatusCode);
        }
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task EnsureFamilyAdminAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

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

    private static string? NormalizeOptionalCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return NormalizeRequiredCategory(category);
    }

    private static string NormalizeRequiredCategory(string category)
    {
        if (CommentTemplateCategories.TryNormalize(category, out var normalizedCategory))
        {
            return normalizedCategory;
        }

        throw new ValidationException(
            new Dictionary<string, string[]>
            {
                ["Category"] = new[]
                {
                    $"Category must be one of: {string.Join(", ", CommentTemplateCategories.AllowedValues)}."
                }
            });
    }

    private static CommentTemplateDto ToDto(CommentTemplate commentTemplate)
    {
        return new CommentTemplateDto(
            commentTemplate.TemplateId,
            commentTemplate.FamilyId,
            commentTemplate.TemplateText,
            commentTemplate.Category,
            commentTemplate.IsSystem,
            commentTemplate.SortOrder);
    }
}
