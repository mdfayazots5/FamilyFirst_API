namespace FamilyFirst.Application.DTOs.Family;

public sealed record FamilyDto(
    Guid FamilyId,
    string FamilyName,
    string? City,
    Guid FamilyAdminUserId,
    int FamilyScore,
    int CurrentStreakDays,
    int BestStreakDays,
    string TimezoneId,
    bool IsActive);
