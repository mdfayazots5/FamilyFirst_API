namespace FamilyFirst.Application.DTOs.Family;

public sealed class CreateFamilyRequest
{
    public string FamilyName { get; init; } = string.Empty;

    public string? City { get; init; }
}
