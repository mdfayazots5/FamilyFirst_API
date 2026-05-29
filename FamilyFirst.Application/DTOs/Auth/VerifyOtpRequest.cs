namespace FamilyFirst.Application.DTOs.Auth;

public sealed class VerifyOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;

    public string OtpToken { get; init; } = string.Empty;

    public string OtpCode { get; init; } = string.Empty;
}
