using System.Text.Json;
using System.Threading.RateLimiting;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyFirst.API.Configuration;

/// <summary>
/// Per-IP throttling for anonymous credential endpoints (R2). Complements the per-account lockout in
/// AuthService and the per-phone OTP quota (3/hr). Limit: RateLimiting:AuthPermitPerMinute (default 10).
/// The partition key is the connection IP; behind a reverse proxy, configure ForwardedHeaders first [VERIFY deployment].
/// </summary>
public static class AuthRateLimiting
{
    public const string CredentialPolicy = "auth-credentials";
    private const int DefaultPermitPerMinute = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue("RateLimiting:AuthPermitPerMinute", DefaultPermitPerMinute);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(CredentialPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.OnRejected = async (rejection, cancellationToken) =>
            {
                var http = rejection.HttpContext;
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                var errorCodes = http.RequestServices.GetRequiredService<IErrorCodeService>();
                var message = await errorCodes.GetMessageAsync(FamilyFirstErrorCode.Too_Many_Requests, cancellationToken: cancellationToken);
                http.Response.ContentType = "application/json";
                var body = ApiResponse<object>.Failure(FamilyFirstErrorCode.Too_Many_Requests.ToString(), message);
                await http.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions), cancellationToken);
            };
        });
    }
}
