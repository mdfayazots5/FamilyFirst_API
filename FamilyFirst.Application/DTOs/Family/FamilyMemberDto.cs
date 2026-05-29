using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.DTOs.Family;

public sealed record FamilyMemberDto(
    Guid FamilyMemberId,
    Guid FamilyId,
    Guid UserId,
    UserRole Role,
    string LinkType,
    string? DisplayName,
    string FullName,
    string PhoneNumber,
    bool IsActive,
    DateTime JoinedAt);
