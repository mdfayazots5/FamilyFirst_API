namespace FamilyFirst.Application.DTOs.Admin;

public sealed class UpdatePlanRequest
{
    public string PlanName { get; init; } = string.Empty;

    public decimal PriceMonthly { get; init; }

    public int MaxChildren { get; init; }

    public int MaxTeachers { get; init; }

    public bool HasElderMode { get; init; }

    public bool HasWeeklyDigest { get; init; }

    public bool HasAdvancedReports { get; init; }

    public int StorageQuotaMb { get; init; }

    public int TrialDays { get; init; }

    public bool IsActive { get; init; } = true;
}
