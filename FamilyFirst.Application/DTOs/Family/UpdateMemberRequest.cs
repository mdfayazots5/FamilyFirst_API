using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Family;

public sealed class UpdateMemberRequest
{
    public UserRole Role { get; init; }

    public string? LinkType { get; init; }

    public string? DisplayName { get; init; }
}
