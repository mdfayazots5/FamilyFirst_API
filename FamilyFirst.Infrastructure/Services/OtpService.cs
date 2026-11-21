using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Services;

public sealed class OtpService(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment,
    IOtpStore store,
    IErrorCodeService errorCodeService,
    ILogger<OtpService> logger) : IOtpService
{
    public async Task<string> SendOtpAsync(string phoneNumber, string countryCode, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Otp:ApiKey"];
        var templateId = configuration["Otp:TemplateId"];
        var hasProvider = IntegrationSettings.IsConfigured(apiKey) && IntegrationSettings.IsConfigured(templateId);
        if (!hasProvider && !environment.IsDevelopment())
        {
            const FamilyFirstErrorCode code = FamilyFirstErrorCode.OTP_Provider_Not_Configured;
            var message = await errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);
            throw new ValidationException(new Dictionary<string, string[]> { [code.ToString()] = [message] }, 503);
        }

        var otpToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        if (!await store.TryCreateAsync(HashPhone(phoneNumber), Hash(otpToken), HashOtp(phoneNumber, otpToken, otpCode), cancellationToken))
            throw new OtpRateLimitException();

        if (hasProvider)
            await SendMsg91OtpAsync(phoneNumber, otpCode, apiKey!, templateId!, cancellationToken);
        else
            logger.LogInformation("Development OTP for {MaskedPhoneNumber}: {OtpCode}", MaskPhoneNumber(phoneNumber), otpCode);

        return otpToken;
    }

    public Task<bool> VerifyOtpAsync(string phoneNumber, string otpToken, string otpCode, CancellationToken cancellationToken)
    {
        return store.TryVerifyAsync(HashPhone(phoneNumber), Hash(otpToken), HashOtp(phoneNumber, otpToken, otpCode), cancellationToken);
    }

    private async Task SendMsg91OtpAsync(
        string phoneNumber, string otpCode, string apiKey, string templateId, CancellationToken cancellationToken)
    {
        var mobile = phoneNumber.TrimStart('+');
        // The auth key travels in the `authkey` header, not the query string, so it never lands in proxy/server
        // URL logs [VERIFY against the MSG91 v5 OTP API docs before go-live — P12].
        var requestUri = $"https://control.msg91.com/api/v5/otp?template_id={Uri.EscapeDataString(templateId)}&mobile={Uri.EscapeDataString(mobile)}&otp={Uri.EscapeDataString(otpCode)}";

        bool accepted;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("authkey", apiKey);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            accepted = false;
            if (response.IsSuccessStatusCode)
            {
                await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
                accepted = json.RootElement.ValueKind == JsonValueKind.Object &&
                    json.RootElement.TryGetProperty("type", out var status) &&
                    status.ValueKind == JsonValueKind.String && status.GetString() == "success";
            }
            if (!accepted)
                logger.LogWarning("MSG91 rejected OTP delivery for {MaskedPhoneNumber}. HTTP {StatusCode}.", MaskPhoneNumber(phoneNumber), response.StatusCode);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // The provider URI and body can contain credentials and the OTP.
            logger.LogWarning("MSG91 OTP delivery failed for {MaskedPhoneNumber}. Failure: {FailureType}.",
                MaskPhoneNumber(phoneNumber), exception.GetType().Name);
            accepted = false;
        }
        if (!accepted)
        {
            const FamilyFirstErrorCode code = FamilyFirstErrorCode.Technical_Error;
            var message = await errorCodeService.GetMessageAsync(code, cancellationToken: cancellationToken);
            throw new ValidationException(new Dictionary<string, string[]> { [code.ToString()] = [message] }, 503);
        }
    }

    private byte[] HashOtp(string phoneNumber, string otpToken, string otpCode)
    {
        var key = HMACSHA256.HashData(OtpSecret(), "FamilyFirst OTP v1"u8);
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{phoneNumber}:{otpToken}:{otpCode}"));
    }

    // Keyed so the stored phone digest cannot be reversed by enumerating the (small) phone-number space.
    private byte[] HashPhone(string phoneNumber)
    {
        var key = HMACSHA256.HashData(OtpSecret(), "FamilyFirst OTP phone v1"u8);
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(phoneNumber));
    }

    // Otp:HashSecret (dedicated, rotatable without touching JWT) when configured; otherwise the JWT secret.
    private byte[] OtpSecret()
    {
        var dedicated = configuration["Otp:HashSecret"];
        var secret = IntegrationSettings.IsConfigured(dedicated) ? dedicated : configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("Otp:HashSecret or Jwt:Secret must be configured.");
        return Encoding.UTF8.GetBytes(secret);
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static string MaskPhoneNumber(string value) => value.Length > 4 ? $"******{value[^4..]}" : "******";
}
