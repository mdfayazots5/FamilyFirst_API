namespace FamilyFirst.Application.DTOs.Family;

public sealed record FamilyDto(
    Guid FamilyId,
    string FamilyName,
    string JoinCode,
    string? City,
    int PlanId,
    string PlanCode,
    Guid? SubscriptionId,
    Guid FamilyAdminUserId,
    int FamilyScore,
    int CurrentStreakDays,
    int BestStreakDays,
    string TimezoneId,
    bool IsActive);
