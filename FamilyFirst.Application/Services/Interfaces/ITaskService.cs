using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyCollection<TaskItemDto>> ListTasksAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid? childId, DateOnly? date, CancellationToken cancellationToken);

    Task<TaskItemDto> CreateTaskAsync(Guid currentUserId, Guid familyId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskItemDto> UpdateTaskAsync(Guid currentUserId, Guid familyId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteTaskAsync(Guid currentUserId, Guid familyId, Guid taskId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskTemplateDto>> ListSystemTemplatesAsync(Guid currentUserId, string? currentUserRole, string? category, string? ageGroup, CancellationToken cancellationToken);

    Task<TaskTemplateDto> CreateSystemTemplateAsync(Guid currentUserId, string? currentUserRole, CreateTaskTemplateRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskCompletionDto>> ListTaskCompletionsAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid? childId, DateOnly? date, CancellationToken cancellationToken);

    Task<TaskCompletionDto> SubmitTaskCompletionAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, Guid taskId, SubmitTaskCompletionRequest request, CancellationToken cancellationToken);

    Task<TaskCompletionDto> ReviewTaskCompletionAsync(Guid currentUserId, Guid familyId, Guid completionId, ReviewTaskCompletionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskCompletionDto>> ListVerificationQueueAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<BatchApproveResultDto> ApproveAllPendingCompletionsAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken);

    Task<TaskCompletionUploadUrlDto> GenerateTaskCompletionUploadUrlAsync(Guid currentUserId, Guid? currentChildProfileId, Guid familyId, TaskCompletionUploadUrlRequest request, CancellationToken cancellationToken);
}

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskItem>> ListFamilyTasksAsync(Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskItem>> ListSystemTemplatesAsync(string? category, string? ageGroup, CancellationToken cancellationToken);

    Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken);

    Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken);
}

public interface ITaskCompletionRepository
{
    Task<TaskCompletion?> GetByIdAsync(Guid completionId, CancellationToken cancellationToken);

    Task<TaskCompletion?> GetByTaskChildAndDateAsync(Guid taskId, Guid childProfileId, DateOnly scheduledDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskCompletion>> ListByFamilyAsync(Guid familyId, Guid? childProfileId, DateOnly? scheduledDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskCompletion>> ListPendingVerificationAsync(Guid familyId, CancellationToken cancellationToken);

    Task AddAsync(TaskCompletion taskCompletion, CancellationToken cancellationToken);

    Task UpdateAsync(TaskCompletion taskCompletion, CancellationToken cancellationToken);

    Task UpdateWithChildProfileAsync(TaskCompletion taskCompletion, ChildProfile childProfile, CancellationToken cancellationToken);
}

public interface IS3StorageService
{
    Task<TaskCompletionUploadUrlDto> GenerateTaskCompletionUploadUrlAsync(Guid familyId, Guid taskId, CancellationToken cancellationToken);
}
