using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Family;

public sealed class AddMemberRequest
{
    public string PhoneNumber { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string LinkType { get; init; } = string.Empty;
}
