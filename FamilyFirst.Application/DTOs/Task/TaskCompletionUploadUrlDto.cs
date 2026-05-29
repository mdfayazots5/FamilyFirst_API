namespace FamilyFirst.Application.DTOs.Task;

public sealed record TaskCompletionUploadUrlDto(
    Guid TaskId,
    string UploadUrl,
    string ObjectKey,
    DateTime ExpiresAtUtc);
