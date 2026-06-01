using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;
using TaskCompletionStatus = FamilyFirst.Domain.Enums.TaskStatus;

namespace FamilyFirst.Domain.Entities;

public sealed class TaskCompletion : BaseEntity
{
    public long TaskItemId { get; set; }

    public long ChildProfileId { get; set; }

    public long FamilyId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public TaskCompletionStatus Status { get; set; } = TaskCompletionStatus.Pending;

    public string? PhotoUrl { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public long? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public int CoinsAwarded { get; set; }

    public TaskItem? TaskItem { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public Family? Family { get; set; }

    public User? ReviewedByUser { get; set; }
}
