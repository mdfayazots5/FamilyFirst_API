namespace FamilyFirst.Application.Services.Interfaces;

public interface IOtpService
{
    Task<string> SendOtpAsync(string phoneNumber, string countryCode, CancellationToken cancellationToken);

    Task<bool> VerifyOtpAsync(string phoneNumber, string otpToken, string otpCode, CancellationToken cancellationToken);
}
