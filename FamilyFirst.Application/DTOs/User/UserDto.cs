namespace FamilyFirst.Application.DTOs.User;

public sealed record UserDto(
    Guid UserId,
    string PhoneNumber,
    string CountryCode,
    string FullName,
    string? Email,
    string? ProfilePhotoUrl,
    string PreferredLanguage,
    string? FcmToken,
    bool IsPhoneVerified,
    bool IsActive);
