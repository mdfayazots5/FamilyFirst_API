using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class Plan : BaseEntity
{
    public long PlanId => InternalId;

    public string PlanName { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public decimal PriceMonthly { get; set; }

    public int MaxChildren { get; set; }

    public int MaxTeachers { get; set; }

    public bool HasElderMode { get; set; }

    public bool HasWeeklyDigest { get; set; }

    public bool HasAdvancedReports { get; set; }

    public int StorageQuotaMb { get; set; }

    public int TrialDays { get; set; }

    public bool IsActive { get; set; } = true;
}
