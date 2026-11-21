using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.API.Middleware;

/// <summary>
/// Maps the per-phone OTP quota (3 requests/hour, enforced atomically by SqlOtpStore) to HTTP 429.
/// Per-IP throttling of credential endpoints is handled by ASP.NET rate limiting (AuthRateLimiting).
/// </summary>
public sealed class RateLimitingMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OtpRateLimitException) when (!context.Response.HasStarted)
        {
            var errorCodes = context.RequestServices.GetRequiredService<IErrorCodeService>();
            var message = await errorCodes.GetMessageAsync(FamilyFirstErrorCode.OTP_Rate_Limit, cancellationToken: context.RequestAborted);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Failure(FamilyFirstErrorCode.OTP_Rate_Limit.ToString(), message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions), context.RequestAborted);
        }
    }
}
