namespace FamilyFirst.Application.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    string Role);

public sealed record UserDto(
    Guid UserId,
    string PhoneNumber,
    string CountryCode,
    string FullName,
    string? Email,
    string? ProfilePhotoUrl,
    bool IsPhoneVerified,
    bool IsActive,
    string PreferredLanguage,
    string Role);

public sealed record CurrentUserDto(
    Guid UserId,
    string? Name,
    string? PhoneNumber,
    string? Role,
    Guid? FamilyId,
    Guid? FamilyMemberId,
    string? PlanCode,
    Guid? ChildProfileId,
    Guid? TeacherProfileId,
    IReadOnlyCollection<Guid> AssignedChildIds);
