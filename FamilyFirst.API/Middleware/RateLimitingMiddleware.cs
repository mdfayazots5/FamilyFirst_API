using System.Collections.Concurrent;
using System.Text.Json;
using FamilyFirst.Application.Common.Models;

namespace FamilyFirst.API.Middleware;

public sealed class RateLimitingMiddleware
{
    private static readonly ConcurrentDictionary<string, List<DateTime>> OtpRequests = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsSendOtpRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var phoneNumber = await ReadPhoneNumberAsync(context.Request);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            await _next(context);
            return;
        }

        if (IsRateLimited(phoneNumber))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Failure("RateLimitExceeded", "OTP request limit exceeded. Try again later.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
            return;
        }

        await _next(context);
    }

    private static bool IsSendOtpRequest(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && request.Path.Equals("/api/auth/send-otp", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ReadPhoneNumberAsync(HttpRequest request)
    {
        request.EnableBuffering();

        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: request.HttpContext.RequestAborted);
            request.Body.Position = 0;

            return document.RootElement.TryGetProperty("phoneNumber", out var phoneNumber)
                ? phoneNumber.GetString()
                : null;
        }
        catch (JsonException)
        {
            request.Body.Position = 0;
            return null;
        }
    }

    private static bool IsRateLimited(string phoneNumber)
    {
        var utcNow = DateTime.UtcNow;
        var windowStart = utcNow.AddHours(-1);
        var requests = OtpRequests.GetOrAdd(phoneNumber, _ => new List<DateTime>());

        lock (requests)
        {
            requests.RemoveAll(timestamp => timestamp < windowStart);

            if (requests.Count >= 3)
            {
                return true;
            }

            requests.Add(utcNow);
            return false;
        }
    }
}
