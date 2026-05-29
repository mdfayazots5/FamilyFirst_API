using System.Net.Http.Headers;
using System.Net.Http.Json;
using FamilyFirst.Application.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Services;

public sealed class FcmPushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FcmPushNotificationService> _logger;

    public FcmPushNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FcmPushNotificationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
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
            || string.IsNullOrWhiteSpace(projectId)
            || projectId.StartsWith('<'))
        {
            _logger.LogInformation("FCM push skipped because configuration or token is missing. Title: {Title}", title);
            return false;
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogInformation("FCM push skipped because service-account credentials are missing. Title: {Title}", title);
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            message = new
            {
                token = fcmToken,
                notification = new
                {
                    title,
                    body
                },
                data = data
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("FCM push delivery failed. Status: {StatusCode}. Body: {Body}", response.StatusCode, responseBody);
            return false;
        }

        return true;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        GoogleCredential? credential = null;
        var serviceAccountJson = _configuration["Fcm:ServiceAccountJson"];
        var serviceAccountFilePath = _configuration["Fcm:ServiceAccountFilePath"];

        if (!string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            credential = GoogleCredential.FromJson(serviceAccountJson);
        }
        else if (!string.IsNullOrWhiteSpace(serviceAccountFilePath) && File.Exists(serviceAccountFilePath))
        {
            credential = GoogleCredential.FromFile(serviceAccountFilePath);
        }

        if (credential is null)
        {
            return null;
        }

        var scopedCredential = credential.CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

        return await scopedCredential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }
}
