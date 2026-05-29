using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Services;

public sealed class OtpService : IOtpService
{
    private static readonly ConcurrentDictionary<string, OtpEntry> OtpEntries = new();

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    public OtpService(HttpClient httpClient, IConfiguration configuration, ILogger<OtpService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendOtpAsync(string phoneNumber, string countryCode, CancellationToken cancellationToken)
    {
        RemoveExpiredEntries();

        var otpToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otpHash = HashOtp(phoneNumber, otpToken, otpCode);

        OtpEntries[otpToken] = new OtpEntry(phoneNumber, otpHash, DateTime.UtcNow.AddMinutes(5), 0);

        await SendMsg91OtpAsync(phoneNumber, otpCode, cancellationToken);

        return otpToken;
    }

    public Task<bool> VerifyOtpAsync(string phoneNumber, string otpToken, string otpCode, CancellationToken cancellationToken)
    {
        if (!OtpEntries.TryGetValue(otpToken, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAt <= DateTime.UtcNow || !string.Equals(entry.PhoneNumber, phoneNumber, StringComparison.Ordinal))
        {
            OtpEntries.TryRemove(otpToken, out _);
            return Task.FromResult(false);
        }

        if (entry.Attempts >= 5)
        {
            OtpEntries.TryRemove(otpToken, out _);
            return Task.FromResult(false);
        }

        var isValid = CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(entry.OtpHash),
            Convert.FromBase64String(HashOtp(phoneNumber, otpToken, otpCode)));

        if (isValid)
        {
            OtpEntries.TryRemove(otpToken, out _);
            return Task.FromResult(true);
        }

        OtpEntries[otpToken] = entry with { Attempts = entry.Attempts + 1 };
        return Task.FromResult(false);
    }

    private async Task SendMsg91OtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Otp:ApiKey"];
        var templateId = _configuration["Otp:TemplateId"];

        if (string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(templateId)
            || apiKey.StartsWith('<')
            || templateId.StartsWith('<'))
        {
            _logger.LogInformation("MSG91 OTP delivery skipped because configuration is not set. OTP for {PhoneNumber}: {OtpCode}", phoneNumber, otpCode);
            return;
        }

        var normalizedMobile = phoneNumber.TrimStart('+');
        var requestUri = $"https://control.msg91.com/api/v5/otp?template_id={Uri.EscapeDataString(templateId)}&mobile={Uri.EscapeDataString(normalizedMobile)}&authkey={Uri.EscapeDataString(apiKey)}&otp={Uri.EscapeDataString(otpCode)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("MSG91 OTP delivery failed for {PhoneNumber}. Status: {StatusCode}. Body: {Body}", phoneNumber, response.StatusCode, responseBody);
        }
    }

    private static string HashOtp(string phoneNumber, string otpToken, string otpCode)
    {
        var input = Encoding.UTF8.GetBytes($"{phoneNumber}:{otpToken}:{otpCode}");
        return Convert.ToBase64String(SHA256.HashData(input));
    }

    private static void RemoveExpiredEntries()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var (key, value) in OtpEntries)
        {
            if (value.ExpiresAt <= utcNow)
            {
                OtpEntries.TryRemove(key, out _);
            }
        }
    }

    private sealed record OtpEntry(string PhoneNumber, string OtpHash, DateTime ExpiresAt, int Attempts);
}
