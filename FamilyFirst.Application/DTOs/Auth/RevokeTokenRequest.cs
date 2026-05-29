namespace FamilyFirst.Application.DTOs.Auth;

public sealed class RevokeTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
