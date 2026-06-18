using System.Security.Claims;
using System.Text.Json;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.API.Middleware;

public sealed class MaintenanceModeMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAdminRepository adminRepository)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!await adminRepository.IsMaintenanceModeEnabledAsync(context.RequestAborted))
        {
            await _next(context);
            return;
        }

        var currentRole = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role");

        if (string.Equals(currentRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<string>.Failure(
            FamilyFirstErrorCode.Technical_Error.ToString(),
            "The platform is temporarily in maintenance mode.");

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
