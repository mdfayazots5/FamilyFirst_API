using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("families/{familyId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskItemDto>>>> ListTasks(
        Guid familyId,
        [FromQuery] Guid? childId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var tasks = await _taskService.ListTasksAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            date,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<TaskItemDto>>.Success(tasks));
    }

    [HttpGet("families/{familyId:guid}/tasks/completions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskCompletionDto>>>> ListTaskCompletions(
        Guid familyId,
        [FromQuery] Guid? childId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var taskCompletions = await _taskService.ListTaskCompletionsAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            childId,
            date,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<TaskCompletionDto>>.Success(taskCompletions));
    }

    [HttpPost("families/{familyId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> CreateTask(
        Guid familyId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateTaskAsync(GetCurrentUserId(), familyId, request, cancellationToken);

        return Created(
            $"/api/families/{familyId}/tasks/{task.TaskId}",
            ApiResponse<TaskItemDto>.Success(task, "Task created."));
    }

    [HttpPut("families/{familyId:guid}/tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> UpdateTask(
        Guid familyId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.UpdateTaskAsync(GetCurrentUserId(), familyId, taskId, request, cancellationToken);

        return Ok(ApiResponse<TaskItemDto>.Success(task, "Task updated."));
    }

    [HttpDelete("families/{familyId:guid}/tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(
        Guid familyId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var deleted = await _taskService.DeleteTaskAsync(GetCurrentUserId(), familyId, taskId, cancellationToken);

        return Ok(ApiResponse<bool>.Success(deleted, "Task deleted."));
    }

    [HttpPost("families/{familyId:guid}/tasks/{taskId:guid}/completions")]
    public async Task<ActionResult<ApiResponse<TaskCompletionDto>>> SubmitTaskCompletion(
        Guid familyId,
        Guid taskId,
        SubmitTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var taskCompletion = await _taskService.SubmitTaskCompletionAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            taskId,
            request,
            cancellationToken);

        return Created(
            $"/api/families/{familyId}/tasks/completions/{taskCompletion.CompletionId}",
            ApiResponse<TaskCompletionDto>.Success(taskCompletion, "Task completion submitted."));
    }

    [HttpPut("families/{familyId:guid}/tasks/completions/{completionId:guid}/review")]
    public async Task<ActionResult<ApiResponse<TaskCompletionDto>>> ReviewTaskCompletion(
        Guid familyId,
        Guid completionId,
        ReviewTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var taskCompletion = await _taskService.ReviewTaskCompletionAsync(
            GetCurrentUserId(),
            familyId,
            completionId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskCompletionDto>.Success(taskCompletion, "Task completion reviewed."));
    }

    [HttpGet("families/{familyId:guid}/tasks/verification-queue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskCompletionDto>>>> ListVerificationQueue(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var taskCompletions = await _taskService.ListVerificationQueueAsync(
            GetCurrentUserId(),
            familyId,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<TaskCompletionDto>>.Success(taskCompletions));
    }

    [HttpPost("families/{familyId:guid}/tasks/verification-queue/approve-all")]
    public async Task<ActionResult<ApiResponse<BatchApproveResultDto>>> ApproveAllPendingCompletions(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.ApproveAllPendingCompletionsAsync(
            GetCurrentUserId(),
            familyId,
            cancellationToken);

        return Ok(ApiResponse<BatchApproveResultDto>.Success(result, "Pending task completions approved."));
    }

    [HttpPost("families/{familyId:guid}/tasks/completions/upload-url")]
    public async Task<ActionResult<ApiResponse<TaskCompletionUploadUrlDto>>> GetTaskCompletionUploadUrl(
        Guid familyId,
        TaskCompletionUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GenerateTaskCompletionUploadUrlAsync(
            GetCurrentUserId(),
            GetCurrentChildProfileId(),
            familyId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskCompletionUploadUrlDto>.Success(result));
    }

    [HttpGet("admin/task-templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskTemplateDto>>>> ListTaskTemplates(
        [FromQuery] string? category,
        [FromQuery] string? ageGroup,
        CancellationToken cancellationToken)
    {
        var templates = await _taskService.ListSystemTemplatesAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            category,
            ageGroup,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<TaskTemplateDto>>.Success(templates));
    }

    [HttpPost("admin/task-templates")]
    public async Task<ActionResult<ApiResponse<TaskTemplateDto>>> CreateTaskTemplate(
        CreateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _taskService.CreateSystemTemplateAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            request,
            cancellationToken);

        return Created(
            $"/api/admin/task-templates/{template.TemplateId}",
            ApiResponse<TaskTemplateDto>.Success(template, "Task template created."));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }

    private Guid? GetCurrentChildProfileId()
    {
        var childProfileId = User.FindFirstValue("childProfileId");

        return Guid.TryParse(childProfileId, out var parsedChildProfileId) ? parsedChildProfileId : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    }
}
