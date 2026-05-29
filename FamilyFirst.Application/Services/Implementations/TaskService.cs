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

    public TaskService(
        ITaskItemRepository taskItemRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ICoinService coinService,
        ITaskCompletionRepository taskCompletionRepository,
        IPushNotificationService pushNotificationService,
        IS3StorageService s3StorageService)
    {
        _taskItemRepository = taskItemRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _coinService = coinService;
        _taskCompletionRepository = taskCompletionRepository;
        _pushNotificationService = pushNotificationService;
        _s3StorageService = s3StorageService;
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
            throw new ForbiddenAccessException("Parent or Child role is required.");
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Guid? resolvedChildId = childId;

        if (member.Role == UserRole.Child)
        {
            if (!currentChildProfileId.HasValue)
            {
                throw new ForbiddenAccessException("Child profile context is required.");
            }

            if (childId.HasValue && childId != currentChildProfileId.Value)
            {
                throw new ForbiddenAccessException("Child can view only their own tasks.");
            }

            resolvedChildId = currentChildProfileId.Value;
        }
        else if (resolvedChildId.HasValue)
        {
            await EnsureChildInFamilyAsync(resolvedChildId.Value, familyId, cancellationToken);
        }

        var taskItems = await _taskItemRepository.ListFamilyTasksAsync(familyId, cancellationToken);

        return taskItems
            .Where(taskItem => IsVisibleToChild(taskItem, resolvedChildId))
            .Where(taskItem => IsActiveForDate(taskItem, targetDate))
            .OrderBy(taskItem => taskItem.TimeBlock)
            .ThenBy(taskItem => taskItem.TaskName)
            .Select(ToTaskItemDto)
            .ToArray();
    }

    public async Task<TaskItemDto> CreateTaskAsync(
        Guid currentUserId,
        Guid familyId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsureTaskChildBelongsToFamilyAsync(request.ChildProfileId, familyId, cancellationToken);

        var taskItem = new TaskItem
        {
            FamilyId = familyId,
            ChildProfileId = request.ChildProfileId,
            CreatedByUserId = currentUserId,
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
            ActiveFromDate = request.ActiveFromDate,
            IsActive = true,
            IsSystemTemplate = false
        };

        await _taskItemRepository.AddAsync(taskItem, cancellationToken);

        return ToTaskItemDto(taskItem);
    }

    public async Task<TaskItemDto> UpdateTaskAsync(
        Guid currentUserId,
        Guid familyId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);
        await EnsureTaskChildBelongsToFamilyAsync(request.ChildProfileId, familyId, cancellationToken);

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);
        var nextDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        taskItem.ChildProfileId = request.ChildProfileId;
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
        taskItem.ActiveFromDate = request.ActiveFromDate < nextDay ? nextDay : request.ActiveFromDate;

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken);

        return ToTaskItemDto(taskItem);
    }

    public async Task<bool> DeleteTaskAsync(
        Guid currentUserId,
        Guid familyId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await EnsureParentOrFamilyAdminAsync(currentUserId, familyId, cancellationToken);

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);
        taskItem.IsActive = false;
        taskItem.IsDeleted = true;
        taskItem.DeletedAt = DateTime.UtcNow;

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<TaskTemplateDto>> ListSystemTemplatesAsync(
        Guid currentUserId,
        string? currentUserRole,
        string? category,
        string? ageGroup,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserId, currentUserRole);

        var taskTemplates = await _taskItemRepository.ListSystemTemplatesAsync(
            NormalizeOptional(category),
            NormalizeOptional(ageGroup),
            cancellationToken);

        return taskTemplates.Select(ToTaskTemplateDto).ToArray();
    }

    public async Task<TaskTemplateDto> CreateSystemTemplateAsync(
        Guid currentUserId,
        string? currentUserRole,
        CreateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSuperAdmin(currentUserId, currentUserRole);

        var taskTemplate = new TaskItem
        {
            FamilyId = null,
            ChildProfileId = null,
            CreatedByUserId = currentUserId,
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
            ActiveFromDate = request.ActiveFromDate,
            IsActive = true,
            IsSystemTemplate = true,
            TemplateCategory = request.Category.Trim(),
            AgeGroup = NormalizeOptional(request.AgeGroup)
        };

        await _taskItemRepository.AddAsync(taskTemplate, cancellationToken);

        return ToTaskTemplateDto(taskTemplate);
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
            throw new ForbiddenAccessException("Parent or Child role is required.");
        }

        Guid? resolvedChildId = childId;

        if (member.Role == UserRole.Child)
        {
            if (!currentChildProfileId.HasValue)
            {
                throw new ForbiddenAccessException("Child profile context is required.");
            }

            if (childId.HasValue && childId != currentChildProfileId.Value)
            {
                throw new ForbiddenAccessException("Child can view only their own task completions.");
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

        return taskCompletions.Select(ToTaskCompletionDto).ToArray();
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
            throw new ForbiddenAccessException("Child profile context is required.");
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException("Child role is required.");
        }

        var taskItem = await GetFamilyTaskOrThrowAsync(taskId, familyId, cancellationToken);

        if (!IsTaskAssignedToChild(taskItem, currentChildProfileId.Value))
        {
            throw new ForbiddenAccessException("Child can only submit tasks assigned to their profile.");
        }

        if (!IsActiveForDate(taskItem, request.ScheduledDate))
        {
            throw new ForbiddenAccessException("Task is not active for the requested scheduled date.");
        }

        if (await _taskCompletionRepository.GetByTaskChildAndDateAsync(
                taskId,
                currentChildProfileId.Value,
                request.ScheduledDate,
                cancellationToken) is not null)
        {
            throw new ConflictException("Task completion already exists for this task, child, and date.");
        }

        if (taskItem.IsPhotoRequired && string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["PhotoUrl"] = new[] { "PhotoUrl is required for tasks that require photo verification." }
                });
        }

        var childProfile = await GetChildInFamilyOrThrowAsync(currentChildProfileId.Value, familyId, cancellationToken);
        var taskCompletion = new TaskCompletion
        {
            TaskId = taskItem.Id,
            ChildProfileId = childProfile.Id,
            FamilyId = familyId,
            ScheduledDate = request.ScheduledDate,
            Status = TaskStatus.SubmittedForReview,
            PhotoUrl = NormalizeOptional(request.PhotoUrl),
            SubmittedAt = DateTime.UtcNow,
            CoinsAwarded = 0
        };

        await _taskCompletionRepository.AddAsync(taskCompletion, cancellationToken);
        taskCompletion.TaskItem = taskItem;
        taskCompletion.ChildProfile = childProfile;

        await SendTaskSubmissionNotificationAsync(familyId, childProfile, taskItem, cancellationToken);

        return ToTaskCompletionDto(taskCompletion);
    }

    public async Task<TaskCompletionDto> ReviewTaskCompletionAsync(
        Guid currentUserId,
        Guid familyId,
        Guid completionId,
        ReviewTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParentAsync(currentUserId, familyId, cancellationToken);

        var taskCompletion = await GetTaskCompletionInFamilyOrThrowAsync(completionId, familyId, cancellationToken);

        if (taskCompletion.Status != TaskStatus.SubmittedForReview)
        {
            throw new ConflictException("Only submitted task completions can be reviewed.");
        }

        taskCompletion.ReviewedByUserId = currentUserId;
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
                taskCompletion.ChildProfileId,
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

        return ToTaskCompletionDto(taskCompletion);
    }

    public async Task<IReadOnlyCollection<TaskCompletionDto>> ListVerificationQueueAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentAsync(currentUserId, familyId, cancellationToken);

        var taskCompletions = await _taskCompletionRepository.ListPendingVerificationAsync(familyId, cancellationToken);

        return taskCompletions.Select(ToTaskCompletionDto).ToArray();
    }

    public async Task<BatchApproveResultDto> ApproveAllPendingCompletionsAsync(
        Guid currentUserId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureParentAsync(currentUserId, familyId, cancellationToken);

        var pendingTaskCompletions = await _taskCompletionRepository.ListPendingVerificationAsync(familyId, cancellationToken);
        var approvedCount = 0;

        foreach (var taskCompletion in pendingTaskCompletions)
        {
            taskCompletion.Status = TaskStatus.Approved;
            taskCompletion.ReviewedByUserId = currentUserId;
            taskCompletion.ReviewedAt = DateTime.UtcNow;
            taskCompletion.CoinsAwarded = taskCompletion.TaskItem!.CoinValue;
            await _taskCompletionRepository.UpdateAsync(taskCompletion, cancellationToken);
            await _coinService.EarnCoinsAsync(
                currentUserId,
                familyId,
                taskCompletion.ChildProfileId,
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

        return new BatchApproveResultDto(approvedCount);
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
            throw new ForbiddenAccessException("Child profile context is required.");
        }

        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Child)
        {
            throw new ForbiddenAccessException("Child role is required.");
        }

        var taskItem = await GetFamilyTaskOrThrowAsync(request.TaskId, familyId, cancellationToken);

        if (!IsTaskAssignedToChild(taskItem, currentChildProfileId.Value))
        {
            throw new ForbiddenAccessException("Child can only upload for tasks assigned to their profile.");
        }

        return await _s3StorageService.GenerateTaskCompletionUploadUrlAsync(
            familyId,
            request.TaskId,
            cancellationToken);
    }

    private async Task<TaskItem> GetFamilyTaskOrThrowAsync(Guid taskId, Guid familyId, CancellationToken cancellationToken)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), taskId);

        if (taskItem.IsSystemTemplate || taskItem.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(TaskItem), taskId);
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
        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childProfileId);
        }
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("User is not a member of this family.");
    }

    private async Task EnsureParentOrFamilyAdminAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException("Parent or FamilyAdmin role is required.");
        }
    }

    private async Task EnsureParentAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException("Parent role is required.");
        }
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childProfileId, Guid familyId, CancellationToken cancellationToken)
    {
        var childProfile = await _childProfileRepository.GetByIdAsync(childProfileId, cancellationToken);

        if (childProfile is null || childProfile.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(ChildProfile), childProfileId);
        }

        return childProfile;
    }

    private async Task<TaskCompletion> GetTaskCompletionInFamilyOrThrowAsync(
        Guid completionId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var taskCompletion = await _taskCompletionRepository.GetByIdAsync(completionId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskCompletion), completionId);

        if (taskCompletion.FamilyId != familyId)
        {
            throw new NotFoundException(nameof(TaskCompletion), completionId);
        }

        return taskCompletion;
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }

    private static void EnsureSuperAdmin(Guid currentUserId, string? currentUserRole)
    {
        EnsureAuthenticated(currentUserId);

        if (!string.Equals(currentUserRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("SuperAdmin role is required.");
        }
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
        return !taskItem.ChildProfileId.HasValue || taskItem.ChildProfileId == childProfileId;
    }

    private static bool IsActiveForDate(TaskItem taskItem, DateOnly targetDate)
    {
        if (targetDate < taskItem.ActiveFromDate)
        {
            return false;
        }

        if (taskItem.ActiveToDate.HasValue && targetDate > taskItem.ActiveToDate.Value)
        {
            return false;
        }

        if (!taskItem.IsRecurring)
        {
            return targetDate == taskItem.ActiveFromDate;
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
            taskItem.FamilyId ?? Guid.Empty,
            taskItem.ChildProfileId,
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
            taskItem.ActiveFromDate,
            taskItem.ActiveToDate,
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
            taskItem.ActiveFromDate,
            taskItem.TemplateCategory ?? string.Empty,
            taskItem.AgeGroup);
    }

    private static TaskCompletionDto ToTaskCompletionDto(TaskCompletion taskCompletion)
    {
        return new TaskCompletionDto(
            taskCompletion.Id,
            taskCompletion.TaskId,
            taskCompletion.ChildProfileId,
            taskCompletion.FamilyId,
            taskCompletion.ScheduledDate,
            taskCompletion.TaskItem?.TaskName ?? string.Empty,
            taskCompletion.ChildProfile?.User?.FullName
                ?? taskCompletion.ChildProfile?.FamilyMember?.User?.FullName
                ?? string.Empty,
            taskCompletion.Status,
            taskCompletion.PhotoUrl,
            taskCompletion.SubmittedAt,
            taskCompletion.ReviewedByUserId,
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
