namespace FamilyFirst.Application.DTOs.Family;

public sealed record ScoreHistoryDto(
    Guid ChildProfileId,
    DateOnly ScoreDate,
    int StudyScore,
    int CleanlinessScore,
    int DisciplineScore,
    int ScreenControlScore,
    int ResponsibilityScore);
