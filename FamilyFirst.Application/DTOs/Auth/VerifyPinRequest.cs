namespace FamilyFirst.Application.DTOs.Auth;

public sealed class VerifyPinRequest
{
    public Guid UserId { get; init; }

    public string Pin { get; init; } = string.Empty;
}
