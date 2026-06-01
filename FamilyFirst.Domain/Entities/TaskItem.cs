using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class TaskItem : BaseEntity
{
    public long? FamilyId { get; set; }

    public long? ChildProfileId { get; set; }

    public long CreatedByUserId { get; set; }

    public string TaskName { get; set; } = string.Empty;

    public string? Instructions { get; set; }

    public string? IconCode { get; set; }

    public TaskTimeBlock TimeBlock { get; set; }

    public int DurationMinutes { get; set; } = 15;

    public int CoinValue { get; set; } = 10;

    public bool IsPhotoRequired { get; set; }

    public string? PillarTag { get; set; }

    public bool IsRecurring { get; set; } = true;

    public string RecurringDays { get; set; } = "[1,2,3,4,5,6,7]";

    public DateTime ActiveFromDate { get; set; }

    public DateTime? ActiveToDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemTemplate { get; set; }

    public string? TemplateCategory { get; set; }

    public string? AgeGroup { get; set; }

    public Family? Family { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public User? CreatedByUser { get; set; }
}
