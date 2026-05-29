namespace FamilyFirst.Application.DTOs.Admin;

public sealed record AdminPlanDto(
    int PlanId,
    string PlanName,
    string PlanCode,
    decimal PriceMonthly,
    int MaxChildren,
    int MaxTeachers,
    bool HasElderMode,
    bool HasWeeklyDigest,
    bool HasAdvancedReports,
    int StorageQuotaMb,
    int TrialDays,
    bool IsActive);
