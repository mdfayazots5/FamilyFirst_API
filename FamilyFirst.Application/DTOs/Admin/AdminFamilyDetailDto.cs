namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminFamilyDetailDto(
    Guid FamilyId,
    string FamilyName,
    string JoinCode,
    string? City,
    bool IsActive,
    int FamilyScore,
    int CurrentStreakDays,
    DateTime CreatedAt,
    int PlanId,
    string PlanCode,
    string PlanName,
    Guid? SubscriptionId,
    string? SubscriptionStatus,
    DateOnly? TrialEndDate,
    DateOnly? EndDate,
    IReadOnlyCollection<AdminFamilyMemberDto> Members);

public sealed record AdminFamilyMemberDto(
    Guid MemberId,
    Guid UserId,
    string FullName,
    string PhoneNumber,
    string Role,
    bool IsActive,
    DateTime JoinedAt);
