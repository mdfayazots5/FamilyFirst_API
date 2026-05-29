namespace FamilyFirst.Application.DTOs.User;

public sealed class UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? ProfilePhotoUrl { get; init; }

    public string PreferredLanguage { get; init; } = "en";
}
