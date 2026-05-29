namespace FamilyFirst.Application.DTOs.Auth;

public sealed class SetPinRequest
{
    public string Pin { get; init; } = string.Empty;
}
