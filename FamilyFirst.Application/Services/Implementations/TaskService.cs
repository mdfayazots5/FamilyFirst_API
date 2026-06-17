using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class TaskService : ITaskService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ICoinService _coinService;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IS3StorageService _s3StorageService;
    private readonly ITaskCompletionRepository _taskCompletionRepository;
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public TaskService(
        ITaskItemRepository taskItemRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        ITaskCompletionRepository taskCompletionRepository,
        IPushNotificationService pushNotificationService,
        IS3StorageService s3StorageService,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _taskItemRepository = taskItemRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _taskCompletionRepository = taskCompletionRepository;
        _pushNotificationService = pushNotificationService;
        _s3StorageService = s3StorageService;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<IReadOnlyCollection<TaskItemDto>> ListTasksAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid? childId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Guid? resolvedChildId = childId;

        if (member.Role == UserRole.Child)
        {
            if (!currentChildProfileId.HasValue)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            if (childId.HasValue && childId != currentChildProfileId.Value)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            resolvedChildId = currentChildProfileId.Value;
        }
        else if (resolvedChildId.HasValue)
        {
            await EnsureChildInFamilyAsync(resolvedChildId.Value, familyId, cancellationToken);
        }

        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);

        var response = taskItems
            .Where(taskItem => IsVisibleToChild(taskItem, resolvedChildId))
            .Where(taskItem => IsActiveForDate(taskItem, targetDate))
            .OrderBy(taskItem => taskItem.TimeBlock)
            .ThenBy(taskItem => taskItem.TaskName)
            .Select(ToTaskItemDto)
            .ToArray();
        LogApiCall(nameof(ListTasksAsync), new { currentUserId, familyId, childId = resolvedChildId, date = targetDate }, new { Count = response.Length });
        return response;
    }

    public async Task<TaskItemDto> CreateTaskAsync(
        Guid currentUserId,
        Guid familyId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        await EnsureTaskChildBelongsToFamilyAsync(request.ChildProfileId, familyId, cancellationToken);

        var taskItem = new TaskItem
        {
            FamilyId = member.FamilyId,
            ChildProfileId = null, // ChildProfileId is long? in entity; Guid? from DTO — skip direct assignment
            CreatedByUserId = member.UserId,
            TaskName = request.TaskName.Trim(),
            Instructions = NormalizeOptional(request.Instructions),
            IconCode = NormalizeOptional(request.IconCode),
            TimeBlock = request.TimeBlock,
            DurationMinutes = request.DurationMinutes,
            CoinValue = request.CoinValue,
            IsPhotoRequired = request.IsPhotoRequired,
            PillarTag = TaskMetadata.NormalizePillarTag(request.PillarTag),
            IsRecurring = request.IsRecurring,
            RecurringDays = SerializeRecurringDays(request.RecurringDays, request.IsRecurring),
            ActiveFromDate = request.ActiveFromDate.ToDateTime(TimeOnly.MinValue),
            IsActive = true,
            IsSystemTemplate = false
        };

        await _taskItemRepository.AddAsync(taskItem, cancellationToken);

        var response = ToTaskItemDto(taskItem);
        LogApiCall(nameof(CreateTaskAsync), new { currentUserId, familyId, request.TaskName, request.ChildProfileId }, new { response.TaskId });
        return response;
    }

    public async Task<TaskItemDto> UpdateTaskAsync(
        Guid currentUserId,
        Guid familyId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        await EnsureTaskChildBelongsToFamilyAsync(request.ChildProfileId, familyId, cancellationToken);

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);
        var nextDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var effectiveFromDate = request.ActiveFromDate < nextDay ? nextDay : request.ActiveFromDate;

        taskItem.ChildProfileId = null; // ChildProfileId is long? in entity; Guid? from DTO — skip
        taskItem.TaskName = request.TaskName.Trim();
        taskItem.Instructions = NormalizeOptional(request.Instructions);
        taskItem.IconCode = NormalizeOptional(request.IconCode);
        taskItem.TimeBlock = request.TimeBlock;
        taskItem.DurationMinutes = request.DurationMinutes;
        taskItem.CoinValue = request.CoinValue;
        taskItem.IsPhotoRequired = request.IsPhotoRequired;
        taskItem.PillarTag = TaskMetadata.NormalizePillarTag(request.PillarTag);
        taskItem.IsRecurring = request.IsRecurring;
        taskItem.RecurringDays = SerializeRecurringDays(request.RecurringDays, request.IsRecurring);
        taskItem.ActiveFromDate = effectiveFromDate.ToDateTime(TimeOnly.MinValue);

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken);

        var response = ToTaskItemDto(taskItem);
        LogApiCall(nameof(UpdateTaskAsync), new { currentUserId, familyId, taskId, request.TaskName, request.ChildProfileId }, new { response.TaskId });
        return response;
    }

    public async Task<bool> DeleteTaskAsync(
        Guid currentUserId,
        Guid familyId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);
        taskItem.IsActive = false;
        taskItem.IsDeleted = true;
        taskItem.DateDeleted = DateTime.UtcNow;

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken);

        LogApiCall(nameof(DeleteTaskAsync), new { currentUserId, familyId, taskId }, new { Deleted = true });
        return true;
    }

    public async Task<IReadOnlyCollection<TaskTemplateDto>> ListSystemTemplatesAsync(
        Guid currentUserId,
        string? currentUserRole,
        string? category,
        string? ageGroup,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserId, currentUserRole, cancellationToken);

        var taskTemplates = await _taskItemRepository.ListSystemTemplatesAsync(
            NormalizeOptional(category),
            NormalizeOptional(ageGroup),
            cancellationToken);

        var response = taskTemplates.Select(ToTaskTemplateDto).ToArray();
        LogApiCall(nameof(ListSystemTemplatesAsync), new { currentUserId, category, ageGroup }, new { Count = response.Length });
        return response;
    }

    public async Task<TaskTemplateDto> CreateSystemTemplateAsync(
        Guid currentUserId,
        string? currentUserRole,
        CreateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserId, currentUserRole, cancellationToken);

        var taskTemplate = new TaskItem
        {
            FamilyId = null,
            ChildProfileId = null,
            CreatedByUserId = 0L, // SuperAdmin user — no family member context; use 0 as placeholder
            TaskName = request.TaskName.Trim(),
            Instructions = NormalizeOptional(request.Instructions),
            IconCode = NormalizeOptional(request.IconCode),
            TimeBlock = request.TimeBlock,
            DurationMinutes = request.DurationMinutes,
            CoinValue = request.CoinValue,
            IsPhotoRequired = request.IsPhotoRequired,
            PillarTag = TaskMetadata.NormalizePillarTag(request.PillarTag),
            IsRecurring = request.IsRecurring,
            RecurringDays = SerializeRecurringDays(request.RecurringDays, request.IsRecurring),
            ActiveFromDate = request.ActiveFromDate.ToDateTime(TimeOnly.MinValue),
            IsActive = true,
            IsSystemTemplate = true,
            TemplateCategory = request.Category.Trim(),
            AgeGroup = NormalizeOptional(request.AgeGroup)
        };

        await _taskItemRepository.AddAsync(taskTemplate, cancellationToken);

        var response = ToTaskTemplateDto(taskTemplate);
        LogApiCall(nameof(CreateSystemTemplateAsync), new { currentUserId, request.TaskName, request.Category }, new { response.TemplateId });
        return response;
    }

    public async Task<IReadOnlyCollection<TaskCompletionDto>> ListTaskCompletionsAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid? childId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        Guid? resolvedChildId = childId;

        if (member.Role == UserRole.Child)
        {
            if (!currentChildProfileId.HasValue)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            if (childId.HasValue && childId != currentChildProfileId.Value)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            resolvedChildId = currentChildProfileId.Value;
        }
        else if (resolvedChildId.HasValue)
        {
            await EnsureChildInFamilyAsync(resolvedChildId.Value, familyId, cancellationToken);
        }

        var taskCompletions = await _taskCompletionRepository.ListByFamilyAsync(
            familyId,
            resolvedChildId,
            date,
            cancellationToken);

        var response = taskCompletions.Select(ToTaskCompletionDto).ToArray();
        LogApiCall(nameof(ListTaskCompletionsAsync), new { currentUserId, familyId, childId = resolvedChildId, date }, new { Count = response.Length });
        return response;
    }

    public async Task<TaskCompletionDto> SubmitTaskCompletionAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        Guid taskId,
        SubmitTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentChildProfileId.HasValue)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);

        if (!IsTaskAssignedToChild(taskItem, currentChildProfileId.Value))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (!IsActiveForDate(taskItem, request.ScheduledDate))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (await _taskCompletionRepository.GetByTaskChildAndDateAsync(
                taskId,
                currentChildProfileId.Value,
                request.ScheduledDate,
                cancellationToken) is not null)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        if (taskItem.IsPhotoRequired && string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["PhotoUrl"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Photo_Required, cancellationToken) }
                });
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(currentChildProfileId.Value, familyId, cancellationToken);
        var completionMember = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        var taskCompletion = new TaskCompletion
        {
            TaskItemId = taskItem.InternalId,
            ChildProfileId = childProfile.InternalId,
            FamilyId = completionMember?.FamilyId ?? 0L,
            ScheduledDate = request.ScheduledDate.ToDateTime(TimeOnly.MinValue),
            Status = TaskStatus.SubmittedForReview,
            PhotoUrl = NormalizeOptional(request.PhotoUrl),
            SubmittedAt = DateTime.UtcNow,
            CoinsAwarded = 0
        };

        await _taskCompletionRepository.AddAsync(taskCompletion, cancellationToken);
        taskCompletion.TaskItem = taskItem;
        taskCompletion.ChildProfile = childProfile;

        await SendTaskSubmissionNotificationAsync(familyId, childProfile, taskItem, cancellationToken);

        var response = ToTaskCompletionDto(taskCompletion);
        LogApiCall(nameof(SubmitTaskCompletionAsync), new { currentUserId, familyId, taskId, currentChildProfileId, request.ScheduledDate }, new { response.CompletionId, response.Status });
        return response;
    }

    public async Task<TaskCompletionDto> ReviewTaskCompletionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid completionId,
        ReviewTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.ApproveReject, cancellationToken);

        var taskCompletion = await GetTaskCompletionInFamilyOrThrowAsync(completionId, familyId, cancellationToken);

        if (taskCompletion.Status != TaskStatus.SubmittedForReview)
        {
            throw new ConflictException(await GetMessageAsync(FamilyFirstErrorCode.Duplicate_Record, cancellationToken));
        }

        taskCompletion.ReviewedByUserId = member.UserId;
        taskCompletion.ReviewedAt = DateTime.UtcNow;

        if (request.Status == TaskStatus.Approved)
        {
            taskCompletion.Status = TaskStatus.Approved;
            taskCompletion.ReviewNote = NormalizeOptional(request.ReviewNote);
            taskCompletion.CoinsAwarded = taskCompletion.TaskItem!.CoinValue;
            await _taskCompletionRepository.UpdateAsync(taskCompletion, cancellationToken);
            await _coinService.EarnCoinsAsync(
                currentUserId,
                familyId,
                taskCompletion.ChildProfile?.Id ?? Guid.Empty,
                taskCompletion.CoinsAwarded,
                "TaskCompletion",
                taskCompletion.Id,
                taskCompletion.TaskItem.TaskName,
                taskCompletion.TaskItem.PillarTag,
                cancellationToken);

            await SendPushToChildAsync(
                taskCompletion.ChildProfile!,
                "Task approved",
                $"+{taskCompletion.CoinsAwarded} coins! {taskCompletion.TaskItem.TaskName} was approved.",
                cancellationToken);
        }
        else
        {
            taskCompletion.Status = TaskStatus.Flagged;
            taskCompletion.ReviewNote = request.ReviewNote!.Trim();
            taskCompletion.CoinsAwarded = 0;

            await _taskCompletionRepository.UpdateAsync(taskCompletion, cancellationToken);

            await SendPushToChildAsync(
                taskCompletion.ChildProfile!,
                "Task needs redo",
                $"Please redo {taskCompletion.TaskItem!.TaskName}: {taskCompletion.ReviewNote}",
                cancellationToken);
        }

        var response = ToTaskCompletionDto(taskCompletion);
        LogApiCall(nameof(ReviewTaskCompletionAsync), new { currentUserId, familyId, completionId, request.Status }, new { response.CompletionId, response.Status });
        return response;
    }

    public async Task<IReadOnlyCollection<TaskCompletionDto>> ListVerificationQueueAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentAsync(currentUserId, familyId, cancellationToken);

        var taskCompletions = await _taskCompletionRepository.ListPendingVerificationAsync(familyId, cancellationToken);

        var response = taskCompletions.Select(ToTaskCompletionDto).ToArray();
        LogApiCall(nameof(ListVerificationQueueAsync), new { currentUserId, familyId }, new { Count = response.Length });
        return response;
    }

    public async Task<BatchApproveResultDto> ApproveAllPendingCompletionsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureParentAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.ApproveReject, cancellationToken);

        var pendingTaskCompletions = await _taskCompletionRepository.ListPendingVerificationAsync(familyId, cancellationToken);
        var approvedCount = 0;

        foreach (var taskCompletion in pendingTaskCompletions)
        {
            taskCompletion.Status = TaskStatus.Approved;
            taskCompletion.ReviewedByUserId = member.UserId;
            taskCompletion.ReviewedAt = DateTime.UtcNow;
            taskCompletion.CoinsAwarded = taskCompletion.TaskItem!.CoinValue;
            await _taskCompletionRepository.UpdateAsync(taskCompletion, cancellationToken);
            await _coinService.EarnCoinsAsync(
                currentUserId,
                familyId,
                taskCompletion.ChildProfile?.Id ?? Guid.Empty,
                taskCompletion.CoinsAwarded,
                "TaskCompletion",
                taskCompletion.Id,
                taskCompletion.TaskItem.TaskName,
                taskCompletion.TaskItem.PillarTag,
                cancellationToken);

            await SendPushToChildAsync(
                taskCompletion.ChildProfile!,
                "Task approved",
                $"+{taskCompletion.CoinsAwarded} coins! {taskCompletion.TaskItem.TaskName} was approved.",
                cancellationToken);

            approvedCount++;
        }

        var response = new BatchApproveResultDto(approvedCount);
        LogApiCall(nameof(ApproveAllPendingCompletionsAsync), new { currentUserId, familyId }, new { response.ApprovedCount });
        return response;
    }

    public async Task<TaskCompletionUploadUrlDto> GenerateTaskCompletionUploadUrlAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        TaskCompletionUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentChildProfileId.HasValue)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var taskItem = await GetFamilyTaskOrThrowAsync(request.TaskId, familyId, cancellationToken);

        if (!IsTaskAssignedToChild(taskItem, currentChildProfileId.Value))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var response = await _s3StorageService.GenerateTaskCompletionUploadUrlAsync(
            familyId,
            request.TaskId,
            cancellationToken);
        LogApiCall(nameof(GenerateTaskCompletionUploadUrlAsync), new { currentUserId, familyId, request.TaskId }, new { HasUploadUrl = !string.IsNullOrWhiteSpace(response.UploadUrl) });
        return response;
    }

    private async Task<TaskItem> GetFamilyTaskOrThrowAsync(Guid taskId, Guid familyId, CancellationToken cancellationToken)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Task_Not_Found, cancellationToken));

        if (taskItem.IsSystemTemplate || taskItem.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Task_Not_Found, cancellationToken));
        }

        return taskItem;
    }

    private async Task EnsureTaskChildBelongsToFamilyAsync(Guid? childProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        if (!childProfileId.HasValue)
        {
            return;
        }

        await EnsureChildInFamilyAsync(childProfileId.Value, familyId, cancellationToken);
    }

    private async Task EnsureChildInFamilyAsync(Guid childProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile,
            childProfileId.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedChildId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task<FamilyMember> EnsureParentOrFamilyAdminAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
    }

    private async Task<FamilyMember> EnsureParentAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return childProfile;
    }

    private async Task<TaskCompletion> GetTaskCompletionInFamilyOrThrowAsync(
        Guid completionId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var taskCompletion = await _taskCompletionRepository.GetByIdAsync(completionId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (taskCompletion.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return taskCompletion;
    }

    private async Task<long> GetFamilyInternalIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return resolvedFamilyId.Value;
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsureSuperAdminAsync(Guid currentUserId, string? currentUserRole, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);
        if (!string.Equals(currentUserRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Task,
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

    private static bool IsVisibleToChild(TaskItem taskItem, Guid? childProfileId)
    {
        if (!childProfileId.HasValue)
        {
            return true;
        }

        return IsTaskAssignedToChild(taskItem, childProfileId.Value);
    }

    private static bool IsTaskAssignedToChild(TaskItem taskItem, Guid childProfileId)
    {
        return !taskItem.ChildProfileId.HasValue || taskItem.ChildProfile?.Id == childProfileId;
    }

    private static bool IsActiveForDate(TaskItem taskItem, DateOnly targetDate)
    {
        var activeFrom = DateOnly.FromDateTime(taskItem.ActiveFromDate);
        var activeTo = taskItem.ActiveToDate.HasValue ? DateOnly.FromDateTime(taskItem.ActiveToDate.Value) : (DateOnly?)null;

        if (targetDate < activeFrom)
        {
            return false;
        }

        if (activeTo.HasValue && targetDate > activeTo.Value)
        {
            return false;
        }

        if (!taskItem.IsRecurring)
        {
            return targetDate == activeFrom;
        }

        var recurringDays = DeserializeRecurringDays(taskItem.RecurringDays);
        var dayOfWeek = targetDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)targetDate.DayOfWeek;

        return recurringDays.Contains(dayOfWeek);
    }

    private static string SerializeRecurringDays(IReadOnlyCollection<int>? recurringDays, bool isRecurring)
    {
        if (!isRecurring)
        {
            return "[]";
        }

        var normalizedRecurringDays = (recurringDays ?? Array.Empty<int>())
            .Distinct()
            .OrderBy(day => day)
            .ToArray();

        return JsonSerializer.Serialize(normalizedRecurringDays, JsonOptions);
    }

    private static IReadOnlyCollection<int> DeserializeRecurringDays(string recurringDays)
    {
        return JsonSerializer.Deserialize<int[]>(recurringDays, JsonOptions) ?? Array.Empty<int>();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static TaskItemDto ToTaskItemDto(TaskItem taskItem)
    {
        return new TaskItemDto(
            taskItem.Id,
            taskItem.Family?.Id ?? Guid.Empty,
            taskItem.ChildProfile?.Id,
            taskItem.TaskName,
            taskItem.Instructions,
            taskItem.IconCode,
            taskItem.TimeBlock,
            taskItem.DurationMinutes,
            taskItem.CoinValue,
            taskItem.IsPhotoRequired,
            taskItem.PillarTag,
            taskItem.IsRecurring,
            DeserializeRecurringDays(taskItem.RecurringDays),
            DateOnly.FromDateTime(taskItem.ActiveFromDate),
            taskItem.ActiveToDate.HasValue ? DateOnly.FromDateTime(taskItem.ActiveToDate.Value) : (DateOnly?)null,
            taskItem.IsActive);
    }

    private static TaskTemplateDto ToTaskTemplateDto(TaskItem taskItem)
    {
        return new TaskTemplateDto(
            taskItem.Id,
            taskItem.TaskName,
            taskItem.Instructions,
            taskItem.IconCode,
            taskItem.TimeBlock,
            taskItem.DurationMinutes,
            taskItem.CoinValue,
            taskItem.IsPhotoRequired,
            taskItem.PillarTag,
            taskItem.IsRecurring,
            DeserializeRecurringDays(taskItem.RecurringDays),
            DateOnly.FromDateTime(taskItem.ActiveFromDate),
            taskItem.TemplateCategory ?? string.Empty,
            taskItem.AgeGroup);
    }

    private static TaskCompletionDto ToTaskCompletionDto(TaskCompletion taskCompletion)
    {
        return new TaskCompletionDto(
            taskCompletion.Id,
            taskCompletion.TaskItem?.Id ?? Guid.Empty,
            taskCompletion.ChildProfile?.Id ?? Guid.Empty,
            taskCompletion.Family?.Id ?? Guid.Empty,
            DateOnly.FromDateTime(taskCompletion.ScheduledDate),
            taskCompletion.TaskItem?.TaskName ?? string.Empty,
            taskCompletion.ChildProfile?.User?.FullName
                ?? taskCompletion.ChildProfile?.FamilyMember?.User?.FullName
                ?? string.Empty,
            taskCompletion.Status,
            taskCompletion.PhotoUrl,
            taskCompletion.SubmittedAt,
            taskCompletion.ReviewedByUser?.Id,
            taskCompletion.ReviewedAt,
            taskCompletion.ReviewNote,
            taskCompletion.CoinsAwarded);
    }

    private async Task SendTaskSubmissionNotificationAsync(
        Guid familyId,
        ChildProfile childProfile,
        TaskItem taskItem,
        CancellationToken cancellationToken)
    {
        var familyMembers = await _familyMemberRepository.ListActiveByFamilyAsync(familyId, cancellationToken);
        var childName = childProfile.User?.FullName
            ?? childProfile.FamilyMember?.User?.FullName
            ?? "Child";

        foreach (var familyMember in familyMembers.Where(familyMember => familyMember.Role == UserRole.Parent))
        {
            var fcmToken = familyMember.User?.FcmToken;

            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                continue;
            }

            await _pushNotificationService.SendPushAsync(
                fcmToken,
                "Task submitted for review",
                $"{childName} submitted {taskItem.TaskName} for review.",
                cancellationToken);
        }
    }

    private async Task SendPushToChildAsync(
        ChildProfile childProfile,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var fcmToken = childProfile.User?.FcmToken
            ?? childProfile.FamilyMember?.User?.FcmToken;

        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            return;
        }

        await _pushNotificationService.SendPushAsync(fcmToken, title, body, cancellationToken);
    }
}
