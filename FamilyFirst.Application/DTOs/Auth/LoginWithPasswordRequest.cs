namespace FamilyFirst.Application.DTOs.Auth;

public sealed record LoginWithPasswordRequest(
    string PhoneNumber,
    string CountryCode,
    string Password);
