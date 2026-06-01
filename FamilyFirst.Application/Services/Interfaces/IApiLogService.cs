namespace FamilyFirst.Application.Services.Interfaces;

/// <summary>
/// Fire-and-forget async API logging service.
/// Every service method calls LogAsync at the end — wraps uspInsertAPILog.
/// The call is fire-and-forget (Task.Run internally) — never blocks the HTTP response.
/// Never throws — all errors are swallowed internally to protect the main response.
/// </summary>
public interface IApiLogService
{
    void Log(
        string methodName,
        string? requestJson,
        string? responseJson,
        long apiMethodId = 0,
        long createdByUserId = 0,
        string? ipAddress = null,
        string? createdBy = null);
}
