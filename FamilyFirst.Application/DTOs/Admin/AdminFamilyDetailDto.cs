namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminFamilyDetailDto(
    Guid FamilyId,
    string FamilyName,
    string? City,
    bool IsActive,
    int FamilyScore,
    int CurrentStreakDays,
    DateTime CreatedAt,
    IReadOnlyCollection<AdminFamilyMemberDto> Members);

public sealed record AdminFamilyMemberDto(
    Guid MemberId,
    Guid UserId,
    string FullName,
    string PhoneNumber,
    string Role,
    bool IsActive,
    DateTime JoinedAt);
