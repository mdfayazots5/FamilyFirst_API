namespace FamilyFirst.Application.DTOs.Family;

public sealed record ChildSummaryDto(
    Guid ChildProfileId,
    Guid FamilyMemberId,
    Guid UserId,
    Guid FamilyId,
    string FullName,
    string? GradeLevel,
    string? SchoolName,
    string AvatarCode,
    int CoinBalance,
    int TotalCoinsEarned,
    int CurrentStreakDays,
    int BestStreakDays,
    int LevelCode,
    int StudyScore,
    int CleanlinessScore,
    int DisciplineScore,
    int ScreenControlScore,
    int ResponsibilityScore);
