using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class CommentTemplateService : ICommentTemplateService
{
    private const int FamilyTemplateLimitPerCategory = 20;
    private const int UnprocessableEntityStatusCode = 422;

    private readonly ICommentTemplateRepository _commentTemplateRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public CommentTemplateService(
        ICommentTemplateRepository commentTemplateRepository,
        IFamilyMemberRepository familyMemberRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _commentTemplateRepository = commentTemplateRepository;
        _familyMemberRepository = familyMemberRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
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
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var normalizedCategory = NormalizeOptionalCategory(category);
        var templates = await _commentTemplateRepository.ListVisibleByFamilyAsync(
            familyId,
            normalizedCategory,
            cancellationToken);

        var response = templates
            .Select(template => new CommentTemplateDto(
                template.Id,
                template.Family?.Id,
                template.TemplateText,
                template.Category,
                template.IsSystem,
                template.SortOrder))
            .ToArray();
        LogApiCall(nameof(ListTemplatesAsync), new { currentUserId, familyId, category = normalizedCategory }, new { Count = response.Length });
        return response;
    }

    public async Task<CommentTemplateDto> CreateTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        CreateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var normalizedCategory = NormalizeRequiredCategory(request.Category);
        await EnsureFamilyTemplateLimitAsync(familyId, normalizedCategory, null, cancellationToken);

        var member = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var commentTemplate = new CommentTemplate
        {
            FamilyId = member?.FamilyId, // long? FK for family
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

        var response = ToDto(commentTemplate);
        LogApiCall(nameof(CreateTemplateAsync), new { currentUserId, familyId, request.Category }, new { response.TemplateId, response.Category });
        return response;
    }

    public async Task<CommentTemplateDto> UpdateTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        Guid templateId,
        UpdateCommentTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.CreateUpdate, cancellationToken);

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

        var response = ToDto(commentTemplate);
        LogApiCall(nameof(UpdateTemplateAsync), new { currentUserId, familyId, templateId, request.Category }, new { response.TemplateId, response.Category });
        return response;
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid currentUserId,
        Guid familyId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        await EnsureFamilyAdminAsync(currentUserId, familyId, FamilyFirstPermission.Delete, cancellationToken);

        var commentTemplate = await GetFamilyTemplateOrThrowAsync(templateId, familyId, cancellationToken);
        commentTemplate.IsActive = false;

        await _commentTemplateRepository.UpdateAsync(commentTemplate, cancellationToken);

        LogApiCall(nameof(DeleteTemplateAsync), new { currentUserId, familyId, templateId }, new { Deleted = true });
        return true;
    }

    private async Task<CommentTemplate> GetFamilyTemplateOrThrowAsync(
        Guid templateId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var commentTemplate = await _commentTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (commentTemplate.IsSystem)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (commentTemplate.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
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
                    ["Category"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                },
                UnprocessableEntityStatusCode);
        }
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task EnsureFamilyAdminAsync(
        Guid currentUserId,
        Guid familyId,
        FamilyFirstPermission permission,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        await EnsurePermissionAsync(member.Role, permission, cancellationToken);
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
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
                    "Category must be one of: " + string.Join(", ", CommentTemplateCategories.AllowedValues) + "."
                }
            });
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Attendance,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await _errorCodeService.GetMessageAsync(
            FamilyFirstErrorCode.Invalid_MasterData,
            cancellationToken: cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(MasterDataCodes)] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }

    private static CommentTemplateDto ToDto(CommentTemplate commentTemplate)
    {
        return new CommentTemplateDto(
            commentTemplate.Id,
            commentTemplate.Family?.Id,
            commentTemplate.TemplateText,
            commentTemplate.Category,
            commentTemplate.IsSystem,
            commentTemplate.SortOrder);
    }
}
