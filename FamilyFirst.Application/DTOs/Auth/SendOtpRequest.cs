namespace FamilyFirst.Application.DTOs.Auth;

public sealed class SendOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;

    public string CountryCode { get; init; } = "+91";
}
