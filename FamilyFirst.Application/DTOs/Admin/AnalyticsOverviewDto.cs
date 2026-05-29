namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AnalyticsOverviewDto(
    int TotalUsers,
    int TotalChildren,
    int TotalTeachers,
    int TotalTasks,
    int TotalTaskCompletions,
    int TotalFeedbackEntries,
    int TotalNotifications);
