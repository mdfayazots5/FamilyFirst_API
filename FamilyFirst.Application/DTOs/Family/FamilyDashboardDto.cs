namespace FamilyFirst.Application.DTOs.Family;

public sealed record FamilyDashboardDto(
    Guid FamilyId,
    string FamilyName,
    DateOnly Date,
    int FamilyScore,
    int CurrentStreakDays,
    int BestStreakDays,
    int UnacknowledgedFeedbackCount,
    int TotalMembers,
    int ParentCount,
    int ChildCount,
    int TeacherCount,
    int ElderCount);
