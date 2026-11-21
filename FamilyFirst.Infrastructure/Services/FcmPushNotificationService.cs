using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Services;

public sealed class FcmPushNotificationService : IPushNotificationService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(400);

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly FcmCredentialCache _credentials;
    private readonly ILogger<FcmPushNotificationService> _logger;

    public FcmPushNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        FcmCredentialCache credentials,
        ILogger<FcmPushNotificationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _credentials = credentials;
        _logger = logger;
    }

    public async Task<bool> SendPushAsync(
        string fcmToken,
        string title,
        string body,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? data = null)
    {
        var projectId = _configuration["Fcm:ProjectId"];

        if (string.IsNullOrWhiteSpace(fcmToken)
            || !IntegrationSettings.IsConfigured(projectId))
        {
            _logger.LogWarning("FCM push skipped because configuration or a device token is missing.");
            return false;
        }

        try
        {
            return await SendConfiguredPushAsync(projectId, fcmToken, title, body, cancellationToken, data);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // SDK exception details can include credential or request data.
            _logger.LogWarning("FCM push failed during authentication or delivery. Failure: {FailureType}.",
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<bool> SendConfiguredPushAsync(
        string projectId, string fcmToken, string title, string body, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? data)
    {
        var accessToken = await _credentials.GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("FCM push skipped because service-account credentials are missing.");
            return false;
        }

        // One retry for transient failures (429 / 5xx / network). Non-transient rejections are not retried.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var response = await _httpClient.SendAsync(
                    BuildRequest(projectId, accessToken, fcmToken, title, body, data), cancellationToken);
                if (response.IsSuccessStatusCode)
                    return true;
                if (attempt >= 2 || !IsTransient(response.StatusCode))
                {
                    _logger.LogWarning("FCM rejected push delivery. HTTP {StatusCode}. Attempts {Attempts}.",
                        response.StatusCode, attempt);
                    return false;
                }
            }
            catch (HttpRequestException) when (attempt < 2)
            {
                // Network failure: fall through to the single retry.
            }

            await Task.Delay(RetryDelay, cancellationToken);
        }
    }

    private static HttpRequestMessage BuildRequest(
        string projectId, string accessToken, string fcmToken, string title, string body,
        IReadOnlyDictionary<string, string>? data)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            message = new
            {
                token = fcmToken,
                notification = new { title, body },
                data
            }
        });
        return request;
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
}
