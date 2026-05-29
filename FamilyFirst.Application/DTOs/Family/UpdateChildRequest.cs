namespace FamilyFirst.Application.DTOs.Family;

public sealed class UpdateChildRequest
{
    public DateOnly? DateOfBirth { get; init; }

    public string? GradeLevel { get; init; }

    public string? SchoolName { get; init; }

    public string AvatarCode { get; init; } = "avatar_01";
}
